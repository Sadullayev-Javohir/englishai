using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Web.Observability;

public sealed class MetricsProtectionMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/metrics"))
        {
            await next(context);
            return;
        }

        var expected = configuration["Observability:MetricsToken"];
        var supplied = context.Request.Headers["X-Metrics-Token"].ToString();
        if (string.IsNullOrWhiteSpace(expected) || !IsPrivate(context.Connection.RemoteIpAddress) ||
            !FixedTimeEquals(expected, supplied))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context);
    }

    public static bool FixedTimeEquals(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }

    public static bool IsPrivate(IPAddress? address)
    {
        if (address is null || IPAddress.IsLoopback(address))
            return true;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
               bytes[0] == 127 ||
               bytes[0] == 192 && bytes[1] == 168 ||
               bytes[0] == 172 && bytes[1] is >= 16 and <= 31;
    }
}
