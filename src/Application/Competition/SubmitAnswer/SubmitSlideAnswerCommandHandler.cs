using Application.Competition.Dtos;
using Application.Competition.Ports;
using Application.Common;
using MediatR;

namespace Application.Competition.SubmitAnswer;

public sealed class SubmitSlideAnswerCommandHandler
    : IRequestHandler<SubmitSlideAnswerCommand, CompetitionDto>
{
    private readonly ICompetitionRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;

    public SubmitSlideAnswerCommandHandler(ICompetitionRepository repo, ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<CompetitionDto> Handle(SubmitSlideAnswerCommand request, CancellationToken cancellationToken)
    {
        var competition = await _repo.GetByIdAsync(request.CompetitionId, cancellationToken)
                          ?? throw new Domain.Common.DomainException("Musobaqa topilmadi.");
        var caller = ResourceOwnership.RequireCurrentLearner(_currentUser);
        var participant = competition.Participants.FirstOrDefault(p => p.Id == request.ParticipantId);
        if (participant is null || participant.LearnerId != caller)
            throw new ForbiddenException("You can only answer as your own participant.");

        competition.SubmitAnswer(
            request.ParticipantId, request.SelectedOptionIndex, request.TimeRatioRemaining);

        await _repo.UpdateAsync(competition, cancellationToken);

        return CompetitionMapper.ToDto(competition);
    }
}
