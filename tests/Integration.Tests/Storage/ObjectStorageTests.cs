using Application.Storage;
using FluentAssertions;
using Infrastructure.Storage;
using Xunit;

namespace Integration.Tests.Storage;

public sealed class ObjectStorageTests
{
    [Fact]
    public void Disabled_storage_allows_empty_configuration()
    {
        var options = new ObjectStorageOptions { Enabled = false };

        FluentActions.Invoking(() => options.Validate(production: true)).Should().NotThrow();
    }

    [Fact]
    public void Enabled_storage_reports_missing_field_names_without_values()
    {
        var options = new ObjectStorageOptions
        {
            Enabled = true,
            AccessKey = "operator-access-key",
            SecretKey = "operator-secret-key",
        };

        var exception = FluentActions.Invoking(() => options.Validate(production: true))
            .Should().Throw<InvalidOperationException>().Which;

        exception.Message.Should().Contain(nameof(ObjectStorageOptions.ServiceUrl));
        exception.Message.Should().Contain(nameof(ObjectStorageOptions.PublicBaseUrl));
        exception.Message.Should().Contain(nameof(ObjectStorageOptions.PublicBucket));
        exception.Message.Should().Contain(nameof(ObjectStorageOptions.PrivateBucket));
        exception.Message.Should().NotContain(options.AccessKey);
        exception.Message.Should().NotContain(options.SecretKey);
    }

    [Fact]
    public void Complete_enabled_storage_configuration_is_valid()
    {
        var options = new ObjectStorageOptions
        {
            Enabled = true,
            ServiceUrl = "https://fsn1.your-objectstorage.com",
            PublicBaseUrl = "https://englishai-public.fsn1.your-objectstorage.com",
            AccessKey = "access",
            SecretKey = "secret",
            PublicBucket = "englishai-public",
            PrivateBucket = "englishai-private",
        };

        FluentActions.Invoking(() => options.Validate(production: true)).Should().NotThrow();
    }

    [Fact]
    public async Task Put_is_idempotent_and_preserves_checksum()
    {
        var storage = new InMemoryObjectStorage();
        var bytes = "RIFF\0\0\0\0WAVE"u8.ToArray();
        var checksum = ObjectKeys.Checksum(bytes);
        var key = ObjectKeys.ListeningAudio("test", Guid.NewGuid(), checksum, "audio/wav");

        await using var first = new MemoryStream(bytes);
        var stored = await storage.PutAsync(new ObjectWriteRequest(key, first, "audio/wav", bytes.Length,
            checksum, ObjectVisibility.Public, "public, max-age=31536000, immutable"), CancellationToken.None);
        await using var second = new MemoryStream(bytes);
        var repeated = await storage.PutAsync(new ObjectWriteRequest(key, second, "audio/wav", bytes.Length,
            checksum, ObjectVisibility.Public, "public, max-age=31536000, immutable"), CancellationToken.None);

        repeated.Key.Should().Be(stored.Key);
        repeated.Checksum.Should().Be(checksum);
        (await storage.HeadAsync(key, ObjectVisibility.Public, CancellationToken.None))!.SizeBytes.Should().Be(bytes.Length);
    }

    [Fact]
    public async Task Local_storage_survives_new_instances()
    {
        var root = Path.Combine(Path.GetTempPath(), $"englishai-storage-{Guid.NewGuid():N}");
        try
        {
            var options = new ObjectStorageOptions { Enabled = false, LocalPath = root };
            var bytes = "persistent image"u8.ToArray();
            var checksum = ObjectKeys.Checksum(bytes);
            const string key = "support/user/conversation/image.png";
            await using (var content = new MemoryStream(bytes))
                await new LocalObjectStorage(options).PutAsync(new ObjectWriteRequest(key, content, "image/png", bytes.Length,
                    checksum, ObjectVisibility.Private, "private, no-store"), CancellationToken.None);

            await using var stored = await new LocalObjectStorage(options).OpenReadAsync(key, ObjectVisibility.Private, CancellationToken.None);
            stored.Should().NotBeNull();
            using var output = new MemoryStream();
            await stored!.Content.CopyToAsync(output);
            output.ToArray().Should().Equal(bytes);
            stored.Metadata.ContentType.Should().Be("image/png");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData("../secret")]
    [InlineData("/absolute")]
    [InlineData("unsafe\\key")]
    public void Unsafe_keys_are_rejected(string key) =>
        FluentActions.Invoking(() => ObjectKeys.Validate(key)).Should().Throw<InvalidDataException>();

    [Fact]
    public void Mime_spoof_is_rejected()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        MediaValidation.DetectImage(png).Should().Be("image/png");
        FluentActions.Invoking(() => MediaValidation.DetectAudio(png)).Should().Throw<InvalidDataException>();
    }
}
