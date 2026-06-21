using Application.Speaking;
using System.Net;
using System.Text.Json;
using Application.Ai;
using Application.Assessment;
using Application.Common;
using Domain.Common;
using FluentValidation;
using Infrastructure.Speaking;

namespace Web.Middleware;

/// <summary>
/// Global error middleware: translates known exception types into clean HTTP
/// responses and prevents stack traces from leaking to clients.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, "Validation failed",
                ex.Errors.Select(e => e.ErrorMessage), code: "validation_failed");
        }
        catch (ArgumentException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, ex.Message, code: "invalid_request");
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, HttpStatusCode.NotFound, ex.Message, code: "not_found");
        }
        catch (PlacementSessionExpiredException ex)
        {
            await WriteAsync(context, HttpStatusCode.Gone, ex.Message, code: PlacementSessionExpiredException.ErrorCode);
        }
        catch (PlacementAudioUnavailableException ex)
        {
            await WriteAsync(context, HttpStatusCode.ServiceUnavailable, ex.Message, code: PlacementAudioUnavailableException.ErrorCode);
        }
        catch (PlacementIntegrityViolationException ex)
        {
            await WriteAsync(context, HttpStatusCode.Conflict, ex.Message, code: PlacementIntegrityViolationException.ErrorCode);
        }
        catch (UnauthorizedException ex)
        {
            // 401: the request could not be authenticated (e.g. an invalid Google token).
            await WriteAsync(context, HttpStatusCode.Unauthorized, ex.Message, code: "unauthorized");
        }
        catch (ForbiddenException ex)
        {
            // 403: authenticated, but trying to reach another learner's data.
            await WriteAsync(context, HttpStatusCode.Forbidden, ex.Message, code: "forbidden");
        }
        catch (SubscriptionRequiredException ex)
        {
            // 402 Payment Required: the free topic trial is used up. The machine code lets the
            // client open the subscribe paywall rather than show a generic error.
            await WriteAsync(context, HttpStatusCode.PaymentRequired, ex.Message, code: ex.Code);
        }
        catch (PaymentsUnavailableException ex)
        {
            // 503 Service Unavailable: online payments are not wired up yet, so the upgrade was
            // refused. The machine code lets the client show a "Premium coming soon" notice.
            await WriteAsync(context, HttpStatusCode.ServiceUnavailable, ex.Message, code: ex.Code);
        }
        catch (FeatureLimitExceededException ex)
        {
            // 402 Payment Required: a free-tier limit was hit; upgrading to Premium lifts it.
            await WriteAsync(context, HttpStatusCode.PaymentRequired, ex.Message, code: "feature_limit_exceeded");
        }
        catch (SpeakingMinutesExhaustedException ex)
        {
            // 402, not 429: the daily speaking budget is an upgrade prompt, not throttling. A 429
            // would land in the client's retry/backoff path and the learner would only see a spinner.
            await WriteAsync(context, HttpStatusCode.PaymentRequired, ex.Message, code: SpeakingMinutesExhaustedException.Code);
        }
        catch (ConflictException ex)
        {
            // 409: the request conflicts with current state (e.g. a username already taken).
            await WriteAsync(context, HttpStatusCode.Conflict, ex.Message, code: "conflict");
        }
        catch (EnergyExhaustedException ex)
        {
            // 409, not 402: an empty energy bar is a "wait for the refill" state, not an upsell.
            // A 402 would make the client open the upgrade paywall instead of the energy modal.
            await WriteAsync(context, HttpStatusCode.Conflict, ex.Message, code: EnergyExhaustedException.CodeValue);
        }
        catch (MandatoryReviewRequiredException ex)
        {
            await WriteAsync(context, (HttpStatusCode)423, ex.Message, code: ex.Code);
        }
        catch (TopicLockedException ex)
        {
            await WriteAsync(context, (HttpStatusCode)423, ex.Message, code: ex.Code);
        }
        catch (AiAdmissionException ex)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, ex.RetryAfterSeconds).ToString();
            await WriteAiAsync(context, ex);
        }
        catch (SpeakingTutorUnavailableException ex)
        {
            await WriteSpeakingUnavailableAsync(context, ex);
        }
        catch (DomainException ex)
        {
            await WriteAsync(context, HttpStatusCode.Conflict, ex.Message, code: "domain_conflict");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
            await WriteAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.", code: "unexpected_error");
        }
    }

    private static Task WriteAiAsync(HttpContext context, AiAdmissionException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception.StatusCode;
        var correlationId = context.Items.TryGetValue(Web.Observability.CorrelationConstants.ItemKey, out var value)
            ? value?.ToString() ?? context.TraceIdentifier
            : context.TraceIdentifier;
        return context.Response.WriteAsJsonAsync(new
        {
            code = exception.Code,
            message = exception.Message,
            retryAfterSeconds = exception.RetryAfterSeconds,
            correlationId,
        }, context.RequestAborted);
    }

    private static Task WriteSpeakingUnavailableAsync(
        HttpContext context,
        SpeakingTutorUnavailableException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return context.Response.WriteAsJsonAsync(new
        {
            code = exception.Code,
            message = exception.Message,
            retryable = exception.Retryable,
        }, context.RequestAborted);
    }

    private static async Task WriteAsync(
        HttpContext context,
        HttpStatusCode status,
        string message,
        IEnumerable<string>? errors = null,
        string? code = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var payload = JsonSerializer.Serialize(new
        {
            status = (int)status,
            message,
            code,
            errors = errors?.ToArray(),
            correlationId = CorrelationId(context)
        });

        await context.Response.WriteAsync(payload);
    }

    private static string CorrelationId(HttpContext context) =>
        context.Items.TryGetValue(Web.Observability.CorrelationConstants.ItemKey, out var value)
            ? value?.ToString() ?? context.TraceIdentifier
            : context.TraceIdentifier;
}
