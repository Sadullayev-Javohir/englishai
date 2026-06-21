using Application.Images.GetTopicImage;
using Application.Images.GetTopicImageManifest;
using MediatR;
using Microsoft.Net.Http.Headers;
using Application.Storage;

namespace Web.Endpoints;

/// <summary>
/// Serves a topic's gallery of images from the database (docs/development-guide.md rule 12 - each licensed image is
/// downloaded once and stored, then re-served from here; the app never hot-links an external host).
/// Every skill that teaches a topic points its <c>&lt;img&gt;</c> at this endpoint, so the stored
/// images illustrate the topic across vocabulary, reading, grammar, writing, listening and video.
/// Slot 0 is the cover; higher slots illustrate the topic in different places. The bytes are
/// immutable per (topic, slot), so the response is cached aggressively; a missing image replies 404
/// and the UI shows a placeholder. The manifest lists which slots exist so the UI can build a gallery.
/// </summary>
public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        // Topic images are public, non-sensitive illustrations loaded by <img> tags and cached
        // aggressively, so they opt out of the global JWT fallback policy - an <img> request must
        // render the same whether or not an auth cookie rides along.
        var group = app.MapGroup("/api/images").WithTags("Images").AllowAnonymous();

        // The cover (slot 0) - the original single-image path catalog cards use.
        group.MapGet("/topics/{topicId:guid}", (Guid topicId, HttpContext http, ISender sender, IObjectStorage storage) =>
            ServeImageAsync(new GetTopicImageQuery(topicId), http, sender, storage));

        // A specific gallery slot (falls back to the cover when that slot is empty).
        group.MapGet("/topics/{topicId:guid}/slots/{slot:int}", (Guid topicId, int slot, HttpContext http, ISender sender, IObjectStorage storage) =>
            ServeImageAsync(new GetTopicImageQuery(topicId, slot), http, sender, storage));

        // A vocabulary word illustration. The opaque wordImageId is derived from topic + English word;
        // its licensed bytes live in the same local store, so learner cards never hot-link providers.
        group.MapGet("/vocabulary-topics/{topicId:guid}/words/{wordImageId:guid}",
            (Guid topicId, Guid wordImageId, HttpContext http, ISender sender, IObjectStorage storage) =>
                ServeImageAsync(new GetTopicImageQuery(wordImageId), http, sender, storage));

        // The gallery manifest: which slots exist + their attribution, so the UI can render a gallery.
        group.MapGet("/topics/{topicId:guid}/manifest", async (Guid topicId, ISender sender) =>
            Results.Ok(await sender.Send(new GetTopicImageManifestQuery(topicId))));

        return app;
    }

    private static async Task<IResult> ServeImageAsync(GetTopicImageQuery query, HttpContext http, ISender sender, IObjectStorage storage)
    {
        var image = await sender.Send(query);
        if (image is null)
            return Results.NotFound();

        // Images are usually stable per (topic, slot) but they CAN be replaced - e.g. when an
        // unsuitable image is swapped out. So the browser must revalidate (cheaply, via the ETag)
        // rather than serve a cached copy blindly: "no-cache" still stores the bytes but rechecks
        // before use, and an unchanged image returns a bodiless 304. A blind max-age would keep a
        // replaced image stale (and visible) in browsers until it expired - unacceptable for content
        // we swap out precisely because it was inappropriate. The ETag is derived from CreatedAt,
        // which changes whenever the image is re-stored, so a replacement invalidates instantly.
        if (image.PublicUrl is { } marker && marker.StartsWith("object:", StringComparison.Ordinal))
        {
            http.Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
            return Results.Redirect(storage.GetPublicUrl(marker[7..]).ToString());
        }
        http.Response.Headers[HeaderNames.CacheControl] = "public, no-cache";
        var etag = image.ETag ?? $"\"{image.CreatedAt.ToUnixTimeSeconds()}\"";
        return Results.File(image.Data!, image.ContentType, lastModified: image.CreatedAt, entityTag:
            new EntityTagHeaderValue(etag));
    }
}
