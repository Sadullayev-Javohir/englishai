using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.Identity.Avatar;

public static class AvatarImageProcessor
{
    private const int Size = 512;

    public static byte[] ToSquareWebp(Stream source)
    {
        try
        {
            using var image = Image.Load(source);
            image.Mutate(context => context.AutoOrient().Resize(new ResizeOptions
            {
                Size = new Size(Size, Size),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
            }));
            using var output = new MemoryStream();
            image.Save(output, new WebpEncoder { Quality = 84, FileFormat = WebpFileFormatType.Lossy });
            return output.ToArray();
        }
        catch (Exception exception) when (exception is InvalidImageContentException or NotSupportedException)
        {
            throw new InvalidDataException("The selected file is not a valid image.", exception);
        }
    }
}
