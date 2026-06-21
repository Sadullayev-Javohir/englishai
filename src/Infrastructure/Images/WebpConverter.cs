using Application.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.Images;

/// <summary>
/// Converts an already-downloaded raster image (JPEG/PNG/…) into a compact
/// <c>image/webp</c> so the bytea cache in <see cref="TopicImage"/> stays small
/// (rule 12 - fetch once, store; WebP typically halves the bytes vs JPEG at equal
/// visual quality). Pure function: never throws on a decode failure - callers pass
/// through the original bytes so a malformed download still lands (degraded, but safe).
/// </summary>
public static class WebpConverter
{
    /// <summary>Quality 1..100 for the lossy WebP encoder. 82 is visually near-lossless
    /// for photos while keeping the file small.</summary>
    private const int WebpQuality = 82;
    private const int MaxWidth = 1280;
    private const int MaxHeight = 960;

    public const string WebpContentType = "image/webp";

    /// <summary>
    /// Re-encodes <paramref name="source"/> as WebP. Returns the original bytes/type when
    /// the input is already WebP or cannot be decoded (callers still persist something).
    /// </summary>
    public static (byte[] Data, string ContentType) ToWebp(byte[] source, string contentType)
    {
        if (source is null || source.Length == 0)
            return (Array.Empty<byte>(), contentType);

        try
        {
            using var image = Image.Load(source);
            var needsResize = image.Width > MaxWidth || image.Height > MaxHeight;
            if (!needsResize
                && string.Equals(contentType, WebpContentType, StringComparison.OrdinalIgnoreCase))
                return (source, contentType);

            if (needsResize)
            {
                image.Mutate(context => context.AutoOrient().Resize(new ResizeOptions
                {
                    Size = new Size(MaxWidth, MaxHeight),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3,
                }));
            }

            var encoder = new WebpEncoder
            {
                // Lossy for photos; near-lossless keeps text/diagram covers crisp without
                // bloating the file. Photos dominate the catalog, so lossy is the default.
                Quality = WebpQuality,
                Method = WebpEncodingMethod.Default,
            };

            using var ms = new MemoryStream();
            image.Save(ms, encoder);
            var webp = ms.ToArray();

            // Storage has one canonical raster format. Keeping a slightly smaller JPEG/PNG here
            // makes the inventory mixed-format and causes word images to be treated as unusable by
            // WordImageBackfillJob, which then downloads the same image again on every pass.
            return (webp, WebpContentType);
        }
        catch (UnknownImageFormatException)
        {
            return (source, contentType);
        }
        catch (ImageFormatException)
        {
            return (source, contentType);
        }
        catch (Exception)
        {
            return (source, contentType);
        }
    }

    /// <summary>Same as <see cref="ToWebp"/> but over a list (used by the re-encode job).</summary>
    public static (byte[] Data, string ContentType) ToWebp(DownloadedImage image) =>
        ToWebp(image.Data, image.ContentType);
}
