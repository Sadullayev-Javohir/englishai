using Domain.Books;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Books;

public class BookProgressTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_passed_section_counts_as_read_a_failed_one_does_not()
    {
        var progress = BookProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();

        progress.RecordSection(s1, correctCount: 8, passed: true, totalSectionCount: 2, Now);
        progress.RecordSection(s2, correctCount: 5, passed: false, totalSectionCount: 2, Now);

        progress.HasPassed(s1).Should().BeTrue();
        progress.HasPassed(s2).Should().BeFalse();
        progress.PassedSectionCount.Should().Be(1);
        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void The_book_completes_once_every_section_is_passed()
    {
        var progress = BookProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();

        progress.RecordSection(s1, 7, passed: true, totalSectionCount: 2, Now);
        progress.IsCompleted.Should().BeFalse();

        progress.RecordSection(s2, 9, passed: true, totalSectionCount: 2, Now);
        progress.IsCompleted.Should().BeTrue();
        progress.CompletedAt.Should().Be(Now);
    }

    [Fact]
    public void Re_attempting_keeps_the_best_score_and_can_turn_a_fail_into_a_pass()
    {
        var progress = BookProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);
        var section = Guid.NewGuid();

        progress.RecordSection(section, 5, passed: false, totalSectionCount: 1, Now);
        progress.RecordSection(section, 9, passed: true, totalSectionCount: 1, Now.AddMinutes(5));

        progress.Sections.Should().ContainSingle();
        progress.Sections.Single().BestCorrectCount.Should().Be(9);
        progress.HasPassed(section).Should().BeTrue();
        progress.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void A_lower_re_attempt_never_lowers_the_recorded_best()
    {
        var progress = BookProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);
        var section = Guid.NewGuid();

        progress.RecordSection(section, 9, passed: true, totalSectionCount: 1, Now);
        progress.RecordSection(section, 4, passed: false, totalSectionCount: 1, Now.AddMinutes(5));

        progress.Sections.Single().BestCorrectCount.Should().Be(9);
        progress.HasPassed(section).Should().BeTrue();
        progress.IsCompleted.Should().BeTrue();
    }
}
