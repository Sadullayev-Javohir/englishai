using Application.Common;
using Application.Reading.Dtos;
using Application.Reading.Ports;
using Application.Vocabulary.Ports;
using Domain.Reading;
using Domain.Vocabulary;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Reading.CheckReadingAnswer;

public sealed class CheckReadingAnswerQueryHandler
    : IRequestHandler<CheckReadingAnswerQuery, ReadingAnswerCheckDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IReadingRepository _passages;

    public CheckReadingAnswerQueryHandler(
        IVocabularyTopicRepository topics,
        IReadingRepository passages)
    {
        _topics = topics;
        _passages = passages;
    }

    public async Task<ReadingAnswerCheckDto> Handle(
        CheckReadingAnswerQuery request,
        CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);
        var passage = await _passages.GetByTopicIdAsync(topic.Id, cancellationToken)
                      ?? throw new NotFoundException("Reading lesson", request.TopicId);
        var question = passage.Questions.SingleOrDefault(item => item.Id == request.QuestionId)
                       ?? throw new NotFoundException(nameof(ReadingQuestion), request.QuestionId);

        if (request.SelectedOptionIndex >= question.Options.Count)
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.SelectedOptionIndex),
                    "Selected option index is out of range."),
            });

        var isCorrect = question.IsCorrect(request.SelectedOptionIndex);
        return new ReadingAnswerCheckDto(
            question.Id,
            request.SelectedOptionIndex,
            question.CorrectOptionIndex,
            isCorrect,
            isCorrect ? null : question.Explanation);
    }
}
