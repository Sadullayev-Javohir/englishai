using FluentAssertions;
using Infrastructure.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Integration.Tests.Images;

public class WebpConverterTests
{
    [Fact]
    public void Large_photo_is_downscaled_and_encoded_as_compact_webp()
    {
        var source = Jpeg(2400, 1600);

        var optimized = WebpConverter.ToWebp(source, "image/jpeg");

        optimized.ContentType.Should().Be(WebpConverter.WebpContentType);
        optimized.Data.Length.Should().BeLessThan(source.Length);
        var info = Image.Identify(optimized.Data);
        info.Width.Should().BeLessThanOrEqualTo(1280);
        info.Height.Should().BeLessThanOrEqualTo(960);
    }

    [Fact]
    public void Small_photo_is_not_upscaled()
    {
        var source = Jpeg(640, 480);

        var optimized = WebpConverter.ToWebp(source, "image/jpeg");

        var info = Image.Identify(optimized.Data);
        info.Width.Should().Be(640);
        info.Height.Should().Be(480);
    }

    [Fact]
    public void Small_photo_is_encoded_as_webp_even_when_webp_is_not_smaller()
    {
        var source = Jpeg(8, 8);

        var optimized = WebpConverter.ToWebp(source, "image/jpeg");

        optimized.ContentType.Should().Be(WebpConverter.WebpContentType);
        optimized.Data.AsSpan(0, 4).ToArray().Should().Equal("RIFF"u8.ToArray());
        optimized.Data.AsSpan(8, 4).ToArray().Should().Equal("WEBP"u8.ToArray());
    }

    private static byte[] Jpeg(int width, int height)
    {
        using var image = new Image<Rgb24>(width, height, new Rgb24(70, 130, 190));
        using var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder { Quality = 95 });
        return stream.ToArray();
    }
}
