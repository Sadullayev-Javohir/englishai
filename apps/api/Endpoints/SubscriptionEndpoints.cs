using Application.Subscription.CancelSubscription;
using Application.Subscription.CheckFeatureAccess;
using Application.Subscription.Click;
using Application.Subscription.ConfirmPayment;
using Application.Subscription.GetPlanCatalog;
using Application.Subscription.GetSubscription;
using Application.Subscription.JoinPremiumWaitlist;
using Application.Subscription.Ports;
using Application.Subscription.StartSubscription;
using Domain.Subscription;
using Infrastructure.Subscription;
using MediatR;
using Microsoft.Extensions.Options;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for subscriptions and payments (PROJECT-SPEC Qism H). Thin: forwards to
/// MediatR, no business logic. The confirm endpoint is the provider webhook target.
/// </summary>
public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscription").WithTags("Subscription");

        // Anonymous: the landing and pricing pages are public, and this is the single source the
        // client reads prices from - previously they were hand-copied into four places in the UI.
        group.MapGet("/plans", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetPlanCatalogQuery()))).AllowAnonymous();

        // Anonymous: a visitor who has not signed up is exactly the demand signal worth capturing
        // while checkout is closed. Always 202 - never reveal whether the contact was already on the
        // list (that would make this an enumeration oracle), and never log the contact value.
        // Throttling comes from the app-wide perimeter limiter; this path is not on its exempt list.
        group.MapPost("/waitlist", async (JoinPremiumWaitlistCommand command, ISender sender) =>
            {
                await sender.Send(command);
                return Results.Accepted();
            })
            .AllowAnonymous();

        group.MapGet("/{learnerId:guid}", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetSubscriptionQuery(learnerId))));

        // Lets the UI show remaining quota / upgrade prompts before attempting an action.
        group.MapGet("/{learnerId:guid}/access/{feature}", async (
            Guid learnerId, PremiumFeature feature, ISender sender) =>
            Results.Ok(await sender.Send(new CheckFeatureAccessQuery(learnerId, feature))));

        group.MapGet("/payments/providers", (IPaymentGatewayResolver gateways, IOptions<RegionalPaymentOptions> options) =>
            Results.Ok(gateways.AvailableProviders
                .Where(provider => provider != PaymentProvider.Local)
                .Select(provider => new PaymentProviderDto(provider, provider.ToString(), options.Value.MockMode))));

        group.MapPost("/{learnerId:guid}/start", async (
            Guid learnerId,
            StartSubscriptionRequest body,
            HttpContext context,
            IOptions<RegionalPaymentOptions> options,
            ISender sender) =>
        {
            var returnUrl = string.IsNullOrWhiteSpace(body.ReturnUrl)
                ? $"{options.Value.PublicBaseUrl.TrimEnd('/')}/profile?payment=success"
                : body.ReturnUrl;
            var webhookBaseUrl = string.IsNullOrWhiteSpace(options.Value.WebhookBaseUrl)
                ? $"{context.Request.Scheme}://{context.Request.Host}"
                : options.Value.WebhookBaseUrl;
            return Results.Ok(await sender.Send(new StartSubscriptionCommand(
                learnerId,
                body.Plan,
                body.Provider,
                returnUrl,
                webhookBaseUrl,
                body.DiscountCode)));
        });

        group.MapPost("/{learnerId:guid}/cancel", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new CancelSubscriptionCommand(learnerId))));

        group.MapPost("/payments/webhook/{provider}", async (
            string provider,
            PaymentWebhookRequest body,
            IPaymentWebhookVerifier verifier,
            ISender sender) =>
        {
            if (!Enum.TryParse<PaymentProvider>(provider, true, out var paymentProvider) ||
                paymentProvider == PaymentProvider.Local)
                return Results.BadRequest(new { message = "Unknown payment provider." });
            if (!string.Equals(body.Status, "paid", StringComparison.OrdinalIgnoreCase))
                return Results.Ok(new { accepted = true, completed = false });
            if (!verifier.Verify(paymentProvider, body.TransactionId, body.Status, body.Signature))
                return Results.Unauthorized();

            var subscription = await sender.Send(new ConfirmPaymentCommand(body.TransactionId));
            return Results.Ok(new { accepted = true, completed = true, subscription });
        });

        group.MapPost("/payments/click/prepare", async (
            HttpRequest request,
            IClickShopApiVerifier verifier,
            ISender sender) =>
        {
            var parsed = await ParseClickRequestAsync(request, requiresPrepareId: false);
            if (parsed.ErrorResponse is not null)
                return parsed.ErrorResponse;
            if (!verifier.IsConfigured || !verifier.VerifyPrepare(parsed.Request!))
                return ClickError(parsed.Request?.ClickTransactionId, parsed.Request?.MerchantTransactionId, -1, "SIGN CHECK FAILED!");

            try
            {
                var amount = decimal.Parse(parsed.Request!.Amount, System.Globalization.CultureInfo.InvariantCulture);
                var result = await sender.Send(new ClickPrepareCommand(
                    parsed.Request.ClickTransactionId,
                    parsed.Request.ClickPaymentId,
                    parsed.Request.MerchantTransactionId,
                    amount,
                    parsed.Request.Action,
                    parsed.Request.Error,
                    parsed.Request.ErrorNote));
                return Results.Json(new
                {
                    click_trans_id = parsed.Request.ClickTransactionId,
                    merchant_trans_id = parsed.Request.MerchantTransactionId,
                    merchant_prepare_id = result.MerchantPrepareId,
                    error = 0,
                    error_note = "Success",
                });
            }
            catch (ClickPaymentException exception)
            {
                return ClickError(parsed.Request!.ClickTransactionId, parsed.Request.MerchantTransactionId,
                    exception.ErrorCode, exception.ErrorNote);
            }
        });

        group.MapPost("/payments/click/complete", async (
            HttpRequest request,
            IClickShopApiVerifier verifier,
            ISender sender) =>
        {
            var parsed = await ParseClickRequestAsync(request, requiresPrepareId: true);
            if (parsed.ErrorResponse is not null)
                return parsed.ErrorResponse;
            if (!verifier.IsConfigured || !verifier.VerifyComplete(parsed.Request!))
                return ClickError(parsed.Request?.ClickTransactionId, parsed.Request?.MerchantTransactionId, -1, "SIGN CHECK FAILED!");

            try
            {
                var amount = decimal.Parse(parsed.Request!.Amount, System.Globalization.CultureInfo.InvariantCulture);
                var result = await sender.Send(new ClickCompleteCommand(
                    parsed.Request.ClickTransactionId,
                    parsed.Request.ClickPaymentId,
                    parsed.Request.MerchantTransactionId,
                    parsed.Request.MerchantPrepareId!.Value,
                    amount,
                    parsed.Request.Action,
                    parsed.Request.Error,
                    parsed.Request.ErrorNote));
                return Results.Json(new
                {
                    click_trans_id = parsed.Request.ClickTransactionId,
                    merchant_trans_id = parsed.Request.MerchantTransactionId,
                    merchant_confirm_id = result.MerchantConfirmId,
                    error = 0,
                    error_note = "Success",
                });
            }
            catch (ClickPaymentException exception)
            {
                return ClickError(parsed.Request!.ClickTransactionId, parsed.Request.MerchantTransactionId,
                    exception.ErrorCode, exception.ErrorNote);
            }
        });

        var paymentOptions = app.ServiceProvider.GetRequiredService<IOptions<RegionalPaymentOptions>>().Value;
        var environment = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (paymentOptions.MockMode && (environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            // The local gateway is intentionally development/testing-only. Production provider
            // callbacks require a provider-specific signed webhook and server-to-server
            // reconciliation; until that adapter exists there is no insecure transaction-id path.
            group.MapPost("/payments/confirm", async (ConfirmPaymentRequest? body, string? transactionId, ISender sender) =>
            {
                var txId = body?.TransactionId ?? transactionId;
                return string.IsNullOrWhiteSpace(txId)
                    ? Results.BadRequest(new { message = "transactionId is required." })
                    : Results.Ok(await sender.Send(new ConfirmPaymentCommand(txId)));
            });

            group.MapGet("/payments/mock-checkout", async (
                string? provider,
                string? transactionId,
                string? returnUrl,
                ISender sender) =>
            {
                if (string.IsNullOrWhiteSpace(transactionId))
                    return Results.BadRequest(new { message = "transactionId is required." });

                await sender.Send(new ConfirmPaymentCommand(transactionId));
                var destination = string.IsNullOrWhiteSpace(returnUrl) ? "/profile?payment=success" : returnUrl;
                return Results.Redirect(destination);
            });
        }

        return app;
    }

    private static async Task<(ClickShopApiRequest? Request, IResult? ErrorResponse)> ParseClickRequestAsync(
        HttpRequest request, bool requiresPrepareId)
    {
        if (!request.HasFormContentType)
            return (null, ClickError(null, null, -8, "Error in request from click"));

        var form = await request.ReadFormAsync();
        var amount = form["amount"].ToString();
        var merchantTransactionId = form["merchant_trans_id"].ToString();
        var errorNote = form["error_note"].ToString();
        var signTime = form["sign_time"].ToString();
        var signString = form["sign_string"].ToString();
        if (!long.TryParse(form["click_trans_id"], out var clickTransactionId) ||
            !int.TryParse(form["service_id"], out var serviceId) ||
            !long.TryParse(form["click_paydoc_id"], out var clickPaymentId) ||
            string.IsNullOrWhiteSpace(merchantTransactionId) ||
            !decimal.TryParse(amount, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out _) ||
            !int.TryParse(form["action"], out var action) ||
            !int.TryParse(form["error"], out var error) ||
            string.IsNullOrWhiteSpace(signTime) ||
            string.IsNullOrWhiteSpace(signString))
            return (null, ClickError(clickTransactionId == 0 ? null : clickTransactionId,
                merchantTransactionId, -8, "Error in request from click"));

        int? merchantPrepareId = null;
        if (requiresPrepareId)
        {
            if (!int.TryParse(form["merchant_prepare_id"], out var parsedPrepareId))
                return (null, ClickError(clickTransactionId, merchantTransactionId, -8, "Error in request from click"));
            merchantPrepareId = parsedPrepareId;
        }

        return (new ClickShopApiRequest(
            clickTransactionId,
            serviceId,
            clickPaymentId,
            merchantTransactionId,
            merchantPrepareId,
            amount,
            action,
            error,
            errorNote,
            signTime,
            signString), null);
    }

    private static IResult ClickError(
        long? clickTransactionId, string? merchantTransactionId, int error, string errorNote) =>
        Results.Json(new
        {
            click_trans_id = clickTransactionId,
            merchant_trans_id = merchantTransactionId,
            error,
            error_note = errorNote,
        });
}

public sealed record StartSubscriptionRequest(
    Domain.Subscription.SubscriptionPlan Plan,
    Domain.Subscription.PaymentProvider Provider,
    string? ReturnUrl = null,
    string? DiscountCode = null);

public sealed record ConfirmPaymentRequest(string TransactionId);
public sealed record PaymentWebhookRequest(string TransactionId, string Status, string Signature);
public sealed record PaymentProviderDto(PaymentProvider Provider, string Name, bool IsMock);
