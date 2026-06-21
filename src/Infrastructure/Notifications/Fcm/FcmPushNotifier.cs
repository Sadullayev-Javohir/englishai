using System.Net;
using System.Text;
using System.Text.Json;
using Application.Notifications.Ports;
using Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications.Fcm;

/// <summary>
/// Firebase Cloud Messaging adapter for <see cref="IPushNotifier"/>, talking to the FCM HTTP v1 API
/// with only the BCL (auth via <see cref="GoogleServiceAccountTokenProvider"/>). Resolves the target
/// devices from the <see cref="IDeviceTokenStore"/>, sends one message per token with bounded
/// concurrency, and prunes tokens FCM reports as unregistered/invalid so the set stays reachable.
/// Delivery is best-effort: a failed send is logged, never thrown, so a broadcast/daily reminder is
/// not derailed by push problems (the in-app feed still has the notification).
/// </summary>
public sealed class FcmPushNotifier : IPushNotifier
{
    private const int MaxConcurrency = 10;

    private readonly IDeviceTokenStore _tokens;
    private readonly GoogleServiceAccountTokenProvider _tokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FcmPushNotifier> _logger;
    private readonly string _projectId;

    public FcmPushNotifier(
        IDeviceTokenStore tokens,
        GoogleServiceAccountTokenProvider tokenProvider,
        IHttpClientFactory httpClientFactory,
        string projectId,
        ILogger<FcmPushNotifier> logger)
    {
        _tokens = tokens;
        _tokenProvider = tokenProvider;
        _httpClientFactory = httpClientFactory;
        _projectId = projectId;
        _logger = logger;
    }

    public async Task NotifyUsersAsync(
        IReadOnlyCollection<Guid> userAccountIds, PushMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var devices = await _tokens.GetForUsersAsync(userAccountIds, cancellationToken);
            await SendToDevicesAsync(devices, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM push to users failed; the in-app notification is unaffected.");
        }
    }

    public async Task NotifyAllAsync(PushMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var devices = await _tokens.GetAllAsync(cancellationToken);
            await SendToDevicesAsync(devices, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM broadcast push failed; the in-app notification is unaffected.");
        }
    }

    private async Task SendToDevicesAsync(
        IReadOnlyList<DeviceToken> devices, PushMessage message, CancellationToken cancellationToken)
    {
        if (devices.Count == 0)
            return;

        string accessToken;
        try
        {
            accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Without an access token nothing can be sent; log once and give up (in-app feed still works).
            _logger.LogError(ex, "FCM push skipped: could not obtain an access token.");
            return;
        }

        var invalidTokens = new System.Collections.Concurrent.ConcurrentBag<string>();
        using var gate = new SemaphoreSlim(MaxConcurrency);

        var sends = devices.Select(async device =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var outcome = await SendOneAsync(device.Token, accessToken, message, cancellationToken);
                if (outcome == SendOutcome.InvalidToken)
                    invalidTokens.Add(device.Token);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(sends);

        if (!invalidTokens.IsEmpty)
            await _tokens.RemoveAsync(invalidTokens.ToArray(), cancellationToken);
    }

    private enum SendOutcome { Delivered, InvalidToken, TransientFailure }

    private async Task<SendOutcome> SendOneAsync(
        string token, string accessToken, PushMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var body = BuildMessageBody(token, message);
            var client = _httpClientFactory.CreateClient("fcm");
            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return SendOutcome.Delivered;

            // FCM reports a token that no longer exists as 404 (UNREGISTERED) or 400 (invalid registration);
            // those should be pruned. Other codes (401/429/5xx) are transient and left in place.
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("FCM token pruned (status {Status}).", (int)response.StatusCode);
                return SendOutcome.InvalidToken;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("FCM send failed ({Status}): {Payload}", (int)response.StatusCode, payload);
            return SendOutcome.TransientFailure;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM send threw for a device token.");
            return SendOutcome.TransientFailure;
        }
    }

    /// <summary>
    /// Builds the FCM v1 message. The <c>notification</c> block renders in the status bar (and drives
    /// the app badge on Android); <c>data.link</c> carries the in-app deep link the app opens on tap.
    /// </summary>
    internal static string BuildMessageBody(string token, PushMessage message)
    {
        var data = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(message.LinkUrl))
            data["link"] = message.LinkUrl!;

        var payload = new
        {
            message = new
            {
                token,
                notification = new { title = message.Title, body = message.Body },
                data,
                android = new
                {
                    priority = "high",
                    notification = new
                    {
                        channel_id = "englishai_learning_v2",
                        icon = "ic_stat_parrot",
                        color = "#127A45",
                        sound = "englishai_reminder",
                        notification_count = 1,
                    },
                },
                apns = new
                {
                    payload = new { aps = new { badge = 1, sound = "default" } },
                },
            },
        };

        return JsonSerializer.Serialize(payload);
    }
}
