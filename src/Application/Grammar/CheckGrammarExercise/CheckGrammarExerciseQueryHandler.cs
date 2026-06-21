using Application.Common;
using Application.Grammar.Dtos;
using Application.Grammar.Ports;
using Application.Vocabulary.Ports;
using Domain.Grammar;
using Domain.Vocabulary;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Grammar.CheckGrammarExercise;

public sealed class CheckGrammarExerciseQueryHandler
    : IRequestHandler<CheckGrammarExerciseQuery, GrammarExerciseCheckDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IGrammarRepository _lessons;

    public CheckGrammarExerciseQueryHandler(
        IVocabularyTopicRepository topics,
        IGrammarRepository lessons)
    {
        _topics = topics;
        _lessons = lessons;
    }

    public async Task<GrammarExerciseCheckDto> Handle(
        CheckGrammarExerciseQuery request,
        CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);
        var lesson = await _lessons.GetByTopicIdAsync(topic.Id, cancellationToken)
                     ?? throw new NotFoundException("Grammar lesson", request.TopicId);
        var exercise = lesson.Exercises.SingleOrDefault(item => item.Id == request.ExerciseId)
                       ?? throw new NotFoundException(nameof(GrammarExercise), request.ExerciseId);

        if (request.TextAnswer is null && (request.SelectedOptionIndex < 0 || request.SelectedOptionIndex >= exercise.Options.Count))
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.SelectedOptionIndex),
                    "Selected option index is out of range."),
            });

        var selectedIndex = request.TextAnswer is null ? request.SelectedOptionIndex : exercise.MatchTextAnswer(request.TextAnswer);
        var isCorrect = exercise.IsCorrect(selectedIndex);
        return new GrammarExerciseCheckDto(
            exercise.Id,
            selectedIndex,
            exercise.CorrectOptionIndex,
            isCorrect,
            isCorrect ? null : exercise.Explanation);
    }
}
