using Domain.Common;
using Domain.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Vocabulary;

public class VocabularyItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private static VocabularyItem Learn(VocabularySource source = VocabularySource.Manual) =>
        VocabularyItem.Learn(Learner, "innovation", "yangilik", Now, "An age of innovation.", source);

    [Fact]
    public void Learn_trims_input_and_schedules_first_review()
    {
        var item = VocabularyItem.Learn(Learner, "  perspective ", " nuqtai nazar ", Now);

        item.Word.Should().Be("perspective");
        item.Translation.Should().Be("nuqtai nazar");
        item.Schedule.Stage.Should().Be(ReviewStage.Day3);
        item.IsDue(Now.AddDays(3)).Should().BeTrue();
    }

    [Theory]
    [InlineData("", "yangilik")]
    [InlineData("innovation", "  ")]
    public void Learn_rejects_empty_word_or_translation(string word, string translation)
    {
        var act = () => VocabularyItem.Learn(Learner, word, translation, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Mini_test_type_changes_with_the_stage()
    {
        var item = Learn();
        item.NextMiniTestType().Should().Be(MiniTestType.ClozeChoice);

        item.RecordReview(true, Now.AddDays(3));
        item.NextMiniTestType().Should().Be(MiniTestType.WrittenUsage);

        item.RecordReview(true, Now.AddDays(7));
        item.NextMiniTestType().Should().Be(MiniTestType.SpokenUsage);
    }

    [Fact]
    public void Words_from_video_use_listening_recognition()
    {
        var item = Learn(VocabularySource.Video);

        item.NextMiniTestType().Should().Be(MiniTestType.ListeningRecognition);
    }
}
