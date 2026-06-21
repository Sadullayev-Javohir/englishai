using Application.Grammar.Dtos;
using MediatR;

namespace Application.Grammar.SubmitGrammarExercises;

/// <summary>One answer in a grammar exercise submission.</summary>
public sealed record GrammarExerciseAnswer(Guid ExerciseId, int SelectedOptionIndex, string? TextAnswer = null);

/// <summary>
/// Submits a learner's answers to a topic's grammar exercises for server-side grading
/// (PROJECT-SPEC G.2). When the <see cref="LearnerId"/> has an existing profile, the score is
/// recorded as Grammar skill activity (so it feeds the level-transition formula) and each wrong
/// answer is recorded against the lesson's error category in the heatmap (Qism C.7 integration).
/// The score is always credited toward the topic's Grammar module on the road to mastering all six
/// modules (PROJECT-SPEC K.5) - the topic is the catalog key.
/// </summary>
public sealed record SubmitGrammarExercisesCommand(
    Guid TopicId,
    Guid LearnerId,
    IReadOnlyList<GrammarExerciseAnswer> Answers) : IRequest<GrammarExerciseResultDto>;
