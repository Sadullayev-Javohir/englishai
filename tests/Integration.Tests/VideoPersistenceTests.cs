using Domain.Assessment;
using Domain.Video;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Video;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Round-trips a <see cref="VideoLesson"/> through the real EF Core / PostgreSQL mapping
/// (migrations applied), proving the owned transcript and question collections persist
/// (including the JSON-mapped options) and that the adaptive catalog query translates to
/// SQL. Tagged "Integration" so CI without a Docker daemon skips it.
/// </summary>
[Trait("Category", "Integration")]
public sealed class VideoPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private DbContextOptions<EnglishAiDbContext> Options() =>
        new DbContextOptionsBuilder<EnglishAiDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

    private static VideoLesson Lesson() =>
        VideoLesson.Curate(
            "Q3oItpVa9fs", "Success in AI", "VOA Learning English", 400, "technology", CefrLevel.B2,
            new[]
            {
                TranscriptSegment.Create(0, 5, "To achieve true success in AI,", "Sun'iy intellektda muvaffaqiyatga erishish uchun,"),
                TranscriptSegment.Create(5, 10, "consistency matters most.", "doimiylik eng muhimi."),
            },
            new[]
            {
                ComprehensionQuestion.Create(
                    "What leads to success?", new[] { "Luck", "Consistency", "Money" }, 1, "hint.ai_consistency"),
            },
            CreatedAt);

    [Fact]
    public async Task Video_lesson_with_owned_collections_round_trips_and_catalog_query_works()
    {
        Guid lessonId;

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();

            var lesson = Lesson();
            lessonId = lesson.Id;
            await new EfVideoRepository(context).SaveAsync(lesson, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var repo = new EfVideoRepository(context);
            var loaded = await repo.GetByIdAsync(lessonId, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.Title.Should().Be("Success in AI");
            loaded.Level.Should().Be(CefrLevel.B2);
            loaded.Status.Should().Be(IngestionStatus.Leveled);
            loaded.Transcript.Should().HaveCount(2);
            loaded.Transcript[0].UzbekTranslation.Should().NotBeNull();
            loaded.Questions.Should().ContainSingle();
            loaded.Questions[0].Options.Should().Equal("Luck", "Consistency", "Money");
            loaded.Questions[0].CorrectOptionIndex.Should().Be(1);

            // Adaptive catalog: a B2 learner (±1) sees the lesson; an A1 learner does not.
            var nearby = await repo.GetCatalogForLevelAsync(CefrLevel.B1, 1, CancellationToken.None);
            nearby.Should().ContainSingle(v => v.Id == lessonId);

            var faraway = await repo.GetCatalogForLevelAsync(CefrLevel.A1, 1, CancellationToken.None);
            faraway.Should().BeEmpty();
        }
    }
}
