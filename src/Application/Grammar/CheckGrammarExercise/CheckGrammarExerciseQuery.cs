using Application.Grammar.Dtos;
using MediatR;

namespace Application.Grammar.CheckGrammarExercise;

public sealed record CheckGrammarExerciseQuery(
    Guid TopicId,
    Guid ExerciseId,
    int SelectedOptionIndex,
    string? TextAnswer = null) : IRequest<GrammarExerciseCheckDto>;
