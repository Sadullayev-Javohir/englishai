using Domain.Assessment;

namespace Application.Assessment.Dtos;

/// <summary>The kind of item the placement flow is currently presenting.</summary>
public enum PlacementItemKind
{
    /// <summary>A multiple-choice question (Vocabulary/Grammar, Listening, Reading).</summary>
    MultipleChoice = 0,

    /// <summary>A free-text writing task scored by the writing assessor.</summary>
    Writing = 1,

    /// <summary>A recorded speaking task scored by the speaking assessor.</summary>
    Speaking = 2
}

/// <summary>
/// Unified client-facing view of the next placement item. A multiple-choice item
/// carries <see cref="Options"/> (and, for reading, a <see cref="PassageText"/>; for
/// listening, <see cref="HasAudio"/>). A productive item (Writing/Speaking) carries the
/// task <see cref="Prompt"/> plus its word-count guidance. The correct answer index and
/// the listening audio script are never sent to the browser.
/// </summary>
public sealed record PlacementItemDto(
    PlacementItemKind Kind,
    Guid Id,
    TestStage Stage,
    CefrLevel Difficulty,
    string Prompt,
    IReadOnlyList<string>? Options,
    bool HasAudio,
    string? PassageText,
    int? MinWords,
    int? MaxWords,
    int StageNumber,
    int StageCount,
    int ItemNumberInStage,
    int ItemsInStage,
    int CompletedItems,
    int TotalItems)
{
    public static PlacementItemDto FromQuestion(PlacementQuestion question, PlacementTestSession session)
    {
        var order = OptionShuffle.Order(session.Id, question.Id, question.Options.Count);
        var shuffledOptions = order.Select(i => question.Options[i]).ToList();

        return Create(
            PlacementItemKind.MultipleChoice,
            question.Id,
            question.Stage,
            question.Difficulty,
            question.Prompt,
            shuffledOptions,
            question.HasAudio,
            question.PassageText,
            minWords: null,
            maxWords: null,
            session);
    }

    public static PlacementItemDto FromWritingTask(
        PlacementWritingTask task,
        PlacementTestSession session) =>
        Create(
            PlacementItemKind.Writing,
            task.Id,
            TestStage.Writing,
            task.Difficulty,
            task.Prompt,
            options: null,
            hasAudio: false,
            passageText: null,
            task.MinWords,
            task.MaxWords,
            session);

    public static PlacementItemDto FromSpeakingTask(
        PlacementSpeakingTask task,
        PlacementTestSession session) =>
        Create(
            PlacementItemKind.Speaking,
            task.Id,
            TestStage.Speaking,
            task.Difficulty,
            task.Prompt,
            options: null,
            hasAudio: false,
            passageText: null,
            task.MinWords,
            maxWords: null,
            session);

    private static PlacementItemDto Create(
        PlacementItemKind kind,
        Guid id,
        TestStage stage,
        CefrLevel difficulty,
        string prompt,
        IReadOnlyList<string>? options,
        bool hasAudio,
        string? passageText,
        int? minWords,
        int? maxWords,
        PlacementTestSession session) =>
        new(
            kind,
            id,
            stage,
            difficulty,
            prompt,
            options,
            hasAudio,
            passageText,
            minWords,
            maxWords,
            session.CurrentStageNumber,
            session.StageCount,
            session.QuestionsAnsweredInStage + 1,
            PlacementTestSession.QuestionsPerStage[session.CurrentStage],
            session.CompletedItemCount,
            session.TotalItemCount);
}
