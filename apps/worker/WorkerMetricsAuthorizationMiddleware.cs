using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Worker;

public sealed class WorkerMetricsAuthorizationMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.Equals("/metrics", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var configured = configuration["Observability:MetricsToken"];
        var provided = context.Request.Headers["X-Metrics-Token"].ToString();
        var remote = context.Connection.RemoteIpAddress;
        var privateAddress = remote is not null && (IPAddress.IsLoopback(remote) || IsPrivate(remote));
        var validToken = !string.IsNullOrWhiteSpace(configured) && FixedEquals(configured, provided);

        if (!privateAddress || !validToken)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context);
    }

    private static bool FixedEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 || bytes[0] == 127 ||
               (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }
}
