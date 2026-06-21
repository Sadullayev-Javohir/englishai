using Application.Common;
using Application.Tests.Learning;
using Application.Vocabulary;
using Application.Vocabulary.GetMandatoryReviewStatus;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Vocabulary;

public sealed class MandatoryReviewPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid LearnerId = Guid.NewGuid();

    private readonly IVocabularyRepository _vocabulary = Substitute.For<IVocabularyRepository>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    [Fact]
    public async Task Status_is_open_when_the_learner_has_no_due_words()
    {
        _vocabulary.GetDueSummaryAsync(LearnerId, Now, Arg.Any<CancellationToken>())
            .Returns(new DueReviewSummary(0, 0));
        var policy = new MandatoryReviewPolicy(_vocabulary, _clock);

        var status = await policy.GetStatusAsync(LearnerId, CancellationToken.None);

        status.IsRequired.Should().BeFalse();
        status.DueItemCount.Should().Be(0);
        status.DueTopicCount.Should().Be(0);
    }

    [Fact]
    public async Task Status_counts_due_words_and_distinct_source_topics()
    {
        _vocabulary.GetDueSummaryAsync(LearnerId, Now, Arg.Any<CancellationToken>())
            .Returns(new DueReviewSummary(4, 2));
        var handler = new GetMandatoryReviewStatusQueryHandler(
            new MandatoryReviewPolicy(_vocabulary, _clock));

        var status = await handler.Handle(
            new GetMandatoryReviewStatusQuery(LearnerId), CancellationToken.None);

        status.IsRequired.Should().BeTrue();
        status.DueItemCount.Should().Be(4);
        status.DueTopicCount.Should().Be(2);
    }

    [Fact]
    public async Task EnsureAccess_throws_when_any_review_is_due()
    {
        _vocabulary.GetDueSummaryAsync(LearnerId, Now, Arg.Any<CancellationToken>())
            .Returns(new DueReviewSummary(1, 0));
        var policy = new MandatoryReviewPolicy(_vocabulary, _clock);

        var act = () => policy.EnsureAccessAsync(LearnerId, CancellationToken.None);

        await act.Should().ThrowAsync<MandatoryReviewRequiredException>();
    }
}
