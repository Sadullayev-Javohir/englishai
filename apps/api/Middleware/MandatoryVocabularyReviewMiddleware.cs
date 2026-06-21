namespace Web.Middleware;

public sealed class MandatoryVocabularyReviewMiddleware
{
    private readonly RequestDelegate _next;

    public MandatoryVocabularyReviewMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context) => _next(context);
}
