using Domain.Assessment;
using Domain.Books;
using Domain.Learning;
using FluentAssertions;
using Infrastructure.Books;
using Infrastructure.Learning;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests.Books;

[Trait("Category", "Integration")]
public sealed class BookQuizPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private DbContextOptions<EnglishAiDbContext> Options() =>
        new DbContextOptionsBuilder<EnglishAiDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

    [Fact]
    public async Task Deferred_book_and_profile_changes_commit_together()
    {
        var learnerId = Guid.NewGuid();
        var book = CreateBook();
        var section = book.Sections.Single();

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();
            await context.Books.AddAsync(book);
            var profile = LearnerProfile.CreateAtLevel(learnerId, CefrLevel.B1, Now);
            await context.LearnerProfiles.AddAsync(profile);
            await context.SaveChangesAsync();
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var progressStore = new EfBookProgressStore(context);
            var profileStore = new EfLearnerProfileRepository(context);
            var profile = await profileStore.GetByLearnerIdAsync(learnerId, CancellationToken.None);
            var progress = BookProgress.Start(learnerId, book.Id, Now);
            progress.RecordSection(section.Id, BookSection.QuestionsPerSection, true, 1, Now);
            profile!.RecordActivity(SkillType.Reading, 100, Now);

            await progressStore.SaveAsync(progress, CancellationToken.None);
            await profileStore.TrackAsync(profile, CancellationToken.None);

            await using (var beforeCommit = new EnglishAiDbContext(Options()))
            {
                (await beforeCommit.BookProgress.CountAsync()).Should().Be(0);
                var unchanged = await new EfLearnerProfileRepository(beforeCommit)
                    .GetByLearnerIdAsync(learnerId, CancellationToken.None);
                unchanged!.Activities.Should().BeEmpty();
            }

            await progressStore.CommitAsync(CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var savedProgress = await context.BookProgress.SingleAsync();
            savedProgress.IsCompleted.Should().BeTrue();
            var savedProfile = await new EfLearnerProfileRepository(context)
                .GetByLearnerIdAsync(learnerId, CancellationToken.None);
            savedProfile!.Activities.Should().ContainSingle(activity =>
                activity.Skill == SkillType.Reading && activity.Score == 100);
        }
    }

    private static Book CreateBook()
    {
        var book = Book.Curate(
            Guid.NewGuid(),
            "Reliable Reading",
            "Ishonchli o'qish",
            "EnglishAI",
            "Persistence test book.",
            "testing",
            CefrLevel.B1,
            "test",
            new[] { "Only section" },
            Now);
        var questions = Enumerable.Range(0, BookSection.QuestionsPerSection)
            .Select(index => BookQuestion.Create(
                $"Question {index}?",
                new[] { "A", "B", "C", "D" },
                0,
                "A is correct."))
            .ToArray();
        book.Sections.Single().FillContent("A persisted section body.", questions);
        return book;
    }
}
