using Application.Analytics.Dtos;
using Application.Gamification.Dtos;
using Application.Learning.Dtos;
using System.Diagnostics;
using Domain.Assessment;
using Domain.Learning;
using Infrastructure.Learning;
using Infrastructure.Llm;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Learning;

public class ProgressInsightGeneratorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Strict_valid_codes_return_hermes_result_and_cache_by_snapshot()
    {
        var llm = Substitute.For<ILlmCompletion>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("""{"overallCode":"needs_focus","achievementCodes":["positive_trend"],"skillsToStrengthen":[{"skill":"Speaking","priority":1,"evidenceCode":"lowest_score","actionCode":"practice_speaking"}],"recurringErrors":[{"category":"Articles","priority":1,"actionCode":"drill_articles"}],"habitCode":"habit_building","nextActionCode":"practice_speaking"}""");
        var generator = NewGenerator(llm);

        var first = await generator.GenerateAsync(Snapshot(), CancellationToken.None);
        var second = await generator.GenerateAsync(Snapshot(), CancellationToken.None);

        first.Source.Should().Be(ProgressInsightSource.Hermes);
        second.IsFallback.Should().BeFalse();
        await llm.Received(1).CompleteAsync(
            Arg.Is<string>(prompt => prompt.Contains("Never output Uzbek", StringComparison.Ordinal)),
            Arg.Is<string>(payload => payload.Contains("ErrorsLast30Days", StringComparison.Ordinal) &&
                                      payload.Contains("AddedLast30Days", StringComparison.Ordinal)),
            HermesProgressInsightGenerator.MaxOutputTokens, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("```json\n{}\n```")]
    [InlineData("not json")]
    [InlineData("{\"overallCode\":\"invented\",\"achievementCodes\":[],\"skillsToStrengthen\":[],\"recurringErrors\":[],\"habitCode\":\"habit_building\",\"nextActionCode\":\"start_placement\"}")]
    [InlineData("{\"overallCode\":\"needs_focus\",\"achievementCodes\":[],\"skillsToStrengthen\":[{\"skill\":\"Writing\",\"priority\":1,\"evidenceCode\":\"lowest_score\",\"actionCode\":\"practice_writing\"}],\"recurringErrors\":[],\"habitCode\":\"habit_building\",\"nextActionCode\":\"practice_writing\"}")]
    public async Task Invalid_or_fabricated_output_uses_deterministic_fallback(string response)
    {
        var llm = Substitute.For<ILlmCompletion>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await NewGenerator(llm).GenerateAsync(Snapshot(), CancellationToken.None);

        result.Source.Should().Be(ProgressInsightSource.Local);
        result.IsFallback.Should().BeTrue();
        result.SkillsToStrengthen.First().Skill.Should().Be(SkillType.Speaking);
    }

    [Fact]
    public async Task Changed_snapshot_is_a_cache_miss()
    {
        var llm = Substitute.For<ILlmCompletion>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("""{"overallCode":"needs_focus","achievementCodes":[],"skillsToStrengthen":[{"skill":"Speaking","priority":1,"evidenceCode":"lowest_score","actionCode":"practice_speaking"}],"recurringErrors":[],"habitCode":"habit_building","nextActionCode":"practice_speaking"}""");
        var generator = NewGenerator(llm);

        await generator.GenerateAsync(Snapshot(), CancellationToken.None);
        await generator.GenerateAsync(Snapshot(score: 41), CancellationToken.None);

        await llm.Received(2).CompleteAsync(Arg.Any<string>(), Arg.Any<string>(),
            HermesProgressInsightGenerator.MaxOutputTokens, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cached_result_is_reused_for_one_hour_then_refreshed()
    {
        var llm = Substitute.For<ILlmCompletion>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("""{"overallCode":"needs_focus","achievementCodes":[],"skillsToStrengthen":[{"skill":"Speaking","priority":1,"evidenceCode":"lowest_score","actionCode":"practice_speaking"}],"recurringErrors":[],"habitCode":"habit_building","nextActionCode":"practice_speaking"}""");
        var clock = new AdjustableTimeProvider(Now);
        var generator = NewGenerator(llm, clock);
        var snapshot = Snapshot();

        await generator.GenerateAsync(snapshot, CancellationToken.None);
        clock.Advance(HermesProgressInsightGenerator.CacheTtl - TimeSpan.FromSeconds(1));
        await generator.GenerateAsync(snapshot, CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(2));
        await generator.GenerateAsync(snapshot, CancellationToken.None);

        await llm.Received(2).CompleteAsync(Arg.Any<string>(), Arg.Any<string>(),
            HermesProgressInsightGenerator.MaxOutputTokens, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Concurrent_requests_share_one_ai_generation()
    {
        var llm = Substitute.For<ILlmCompletion>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await release.Task;
                return (string?)"""{"overallCode":"needs_focus","achievementCodes":[],"skillsToStrengthen":[{"skill":"Speaking","priority":1,"evidenceCode":"lowest_score","actionCode":"practice_speaking"}],"recurringErrors":[],"habitCode":"habit_building","nextActionCode":"practice_speaking"}""";
            });
        var generator = NewGenerator(llm);
        var snapshot = Snapshot();

        var first = generator.GenerateAsync(snapshot, CancellationToken.None);
        var second = generator.GenerateAsync(snapshot, CancellationToken.None);
        release.SetResult();
        await Task.WhenAll(first, second);

        await llm.Received(1).CompleteAsync(Arg.Any<string>(), Arg.Any<string>(),
            HermesProgressInsightGenerator.MaxOutputTokens, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Slow_external_ai_falls_back_before_the_regular_api_timeout()
    {
        var llm = Substitute.For<ILlmCompletion>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
                return (string?)"unreachable";
            });
        var stopwatch = Stopwatch.StartNew();

        var result = await NewGenerator(llm).GenerateAsync(Snapshot(), CancellationToken.None);

        stopwatch.Stop();
        result.Source.Should().Be(ProgressInsightSource.Local);
        result.IsFallback.Should().BeTrue();
        stopwatch.Elapsed.Should().BeLessThan(
            TimeSpan.FromSeconds(15),
            "the local fallback must finish inside the frontend's regular request timeout even under full-suite load");
    }

    [Fact]
    public async Task Expected_timeout_is_logged_as_information()
    {
        var llm = Substitute.For<ILlmCompletion>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
                return (string?)null;
            });
        var logger = new CapturingLogger();
        var generator = new HermesProgressInsightGenerator(
            llm, new LocalProgressInsightGenerator(), new FixedTimeProvider(Now), logger);

        await generator.GenerateAsync(Snapshot(), CancellationToken.None);

        logger.Level.Should().Be(LogLevel.Information);
    }

    private static HermesProgressInsightGenerator NewGenerator(ILlmCompletion llm, TimeProvider? clock = null) => new(
        llm, new LocalProgressInsightGenerator(), clock ?? new FixedTimeProvider(Now),
        NullLogger<HermesProgressInsightGenerator>.Instance);

    private static ProgressSnapshotDto Snapshot(double score = 40) => new(
        Guid.NewGuid(), new DateOnly(2026, 7, 22), CefrLevel.B1, null,
        [new ProgressSkillSnapshotDto(SkillType.Speaking, score, 2, ProgressDataConfidence.Low,
            false, 5, [new GrowthPointDto(Now.AddDays(-49), SkillType.Speaking, 35),
                new GrowthPointDto(Now, SkillType.Speaking, score)])],
        [new ProgressErrorSnapshotDto(ErrorCategory.Articles, 3, [])],
        new TopicProgressSummaryDto(2, 1, 5),
        new StudyStatsDto(10, 20, 30, 40, 50, 2, 1, 25, 30, [], [], [], []),
        new VocabularySummaryDto(4, 3, 1, 1, 1, 3, 2, []),
        new GamificationStatusDto(1, 3, false, 1, 2, false, []), true);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }

    private sealed class CapturingLogger : ILogger<HermesProgressInsightGenerator>
    {
        public LogLevel? Level { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Level = logLevel;
    }
}
