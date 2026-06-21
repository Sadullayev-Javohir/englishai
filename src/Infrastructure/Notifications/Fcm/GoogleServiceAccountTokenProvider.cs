using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Notifications.Fcm;

/// <summary>
/// Mints Google OAuth2 access tokens for a service account using the JWT-bearer grant, so the FCM
/// HTTP v1 API can be called with only the BCL (no Firebase SDK dependency). Signs a short-lived
/// assertion with the account's RSA private key, exchanges it at the token endpoint, and caches the
/// resulting access token until shortly before it expires.
/// </summary>
public sealed class GoogleServiceAccountTokenProvider
{
    private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";

    private readonly string _clientEmail;
    private readonly string _privateKeyPem;
    private readonly string _tokenUri;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _cachedExpiry;

    public GoogleServiceAccountTokenProvider(string serviceAccountJson, IHttpClientFactory httpClientFactory)
    {
        using var doc = JsonDocument.Parse(serviceAccountJson);
        var root = doc.RootElement;
        _clientEmail = root.GetProperty("client_email").GetString()
                       ?? throw new InvalidOperationException("Service account JSON is missing client_email.");
        _privateKeyPem = root.GetProperty("private_key").GetString()
                         ?? throw new InvalidOperationException("Service account JSON is missing private_key.");
        _tokenUri = root.TryGetProperty("token_uri", out var uri) && uri.GetString() is { Length: > 0 } t
            ? t
            : "https://oauth2.googleapis.com/token";
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>Returns a valid access token, refreshing (and caching) it when the current one is near expiry.</summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // A 60s safety margin avoids handing out a token that expires mid-request.
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _cachedExpiry - TimeSpan.FromSeconds(60))
            return _cachedToken;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _cachedExpiry - TimeSpan.FromSeconds(60))
                return _cachedToken;

            var assertion = BuildSignedAssertion();
            var client = _httpClientFactory.CreateClient("fcm");
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion,
            });

            using var response = await client.PostAsync(_tokenUri, content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"FCM token exchange failed ({(int)response.StatusCode}): {payload}");

            using var tokenDoc = JsonDocument.Parse(payload);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
                              ?? throw new InvalidOperationException("FCM token response missing access_token.");
            var expiresIn = tokenDoc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;

            _cachedToken = accessToken;
            _cachedExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private string BuildSignedAssertion()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Base64Url(Encoding.UTF8.GetBytes("""{"alg":"RS256","typ":"JWT"}"""));
        var claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["iss"] = _clientEmail,
            ["scope"] = Scope,
            ["aud"] = _tokenUri,
            ["iat"] = now,
            ["exp"] = now + 3600,
        }));

        var signingInput = $"{header}.{claims}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKeyPem);
        var signature = rsa.SignData(
            Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{signingInput}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
