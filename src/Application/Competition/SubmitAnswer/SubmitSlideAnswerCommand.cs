using Application.Competition.Dtos;
using MediatR;

namespace Application.Competition.SubmitAnswer;

/// <summary>A participant submits an answer for the current slide; returns the updated view.</summary>
public sealed record SubmitSlideAnswerCommand(
    Guid CompetitionId,
    Guid ParticipantId,
    int SelectedOptionIndex,
    double TimeRatioRemaining)
    : IRequest<CompetitionDto>;
