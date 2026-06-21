using Application.Common;
using Application.Listening.Dtos;
using Application.Listening.Ports;
using Application.Vocabulary.Ports;
using Domain.Listening;
using Domain.Vocabulary;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Listening.CheckListeningAnswer;

public sealed class CheckListeningAnswerQueryHandler
    : IRequestHandler<CheckListeningAnswerQuery, ListeningAnswerCheckDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IListeningRepository _exercises;
    private readonly IListeningContentProvider _content;

    public CheckListeningAnswerQueryHandler(
        IVocabularyTopicRepository topics,
        IListeningRepository exercises,
        IListeningContentProvider content)
    {
        _topics = topics;
        _exercises = exercises;
        _content = content;
    }

    public async Task<ListeningAnswerCheckDto> Handle(
        CheckListeningAnswerQuery request,
        CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);
        var exercise = await _exercises.GetByTopicIdAsync(topic.Id, cancellationToken)
                       ?? throw new NotFoundException("Listening exercise", request.TopicId);
        var question = exercise.Questions.SingleOrDefault(item => item.Id == request.QuestionId)
                       ?? throw new NotFoundException(nameof(ListeningQuestion), request.QuestionId);

        if (request.SelectedOptionIndex >= question.Options.Count)
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.SelectedOptionIndex),
                    "Selected option index is out of range."),
            });

        var isCorrect = question.IsCorrect(request.SelectedOptionIndex);
        return new ListeningAnswerCheckDto(
            question.Id,
            request.SelectedOptionIndex,
            question.CorrectOptionIndex,
            isCorrect,
            isCorrect ? null : question.Explanation ?? _content.GetHint(question.HintCode));
    }
}
