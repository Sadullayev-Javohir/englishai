namespace Web.Observability;

public sealed class CorrelationIdHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.Items[CorrelationConstants.ItemKey] is string correlationId &&
            !request.Headers.Contains(CorrelationConstants.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(CorrelationConstants.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
