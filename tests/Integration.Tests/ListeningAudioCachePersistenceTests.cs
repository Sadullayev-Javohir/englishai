using Application.Storage;
using FluentAssertions;
using Infrastructure.Listening;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

[Trait("Category", "Integration")]
public sealed class ListeningAudioCachePersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
    private DbContextOptions<EnglishAiDbContext> Options() => new DbContextOptionsBuilder<EnglishAiDbContext>()
        .UseNpgsql(_postgres.GetConnectionString()).Options;

    [Fact]
    public async Task Clip_metadata_persists_and_binary_is_externalized()
    {
        var exerciseId = Guid.NewGuid();
        var audio = "RIFF\0\0\0\0WAVE"u8.ToArray();
        var storage = new InMemoryObjectStorage();
        var options = new ObjectStorageOptions { Enabled = true, KeyPrefix = "test" };

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();
            await new EfListeningAudioCache(context, storage, options)
                .SetAsync(exerciseId, audio, "audio/wav", CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var content = await new EfListeningAudioCache(context, storage, options)
                .GetAsync(exerciseId, CancellationToken.None);
            content.Should().NotBeNull();
            content!.Audio.Should().BeNull();
            content.PublicUrl.Should().NotBeNull();
            content.SizeBytes.Should().Be(audio.Length);
            var row = await context.ListeningAudioClips.SingleAsync();
            row.AudioContent.Should().BeNull();
            row.ObjectKey.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Clip_binary_persists_in_postgres_when_object_storage_is_disabled()
    {
        var exerciseId = Guid.NewGuid();
        var audio = "RIFF\0\0\0\0WAVE"u8.ToArray();
        var storage = new InMemoryObjectStorage();
        var options = new ObjectStorageOptions { Enabled = false, KeyPrefix = "test" };

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();
            await new EfListeningAudioCache(context, storage, options)
                .SetAsync(exerciseId, audio, "audio/wav", CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var content = await new EfListeningAudioCache(context, storage, options)
                .GetAsync(exerciseId, CancellationToken.None);
            content.Should().NotBeNull();
            content!.Audio.Should().Equal(audio);
            content.PublicUrl.Should().BeNull();
            var row = await context.ListeningAudioClips.SingleAsync();
            row.AudioContent.Should().Equal(audio);
            row.ObjectKey.Should().BeNull();
        }
    }
}
