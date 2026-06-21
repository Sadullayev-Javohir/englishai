using Application.Competition.Dtos;
using Application.Competition.Ports;
using Application.Common;
using MediatR;

namespace Application.Competition.Start;

public sealed class StartCompetitionCommandHandler
    : IRequestHandler<StartCompetitionCommand, CompetitionDto>
{
    private readonly ICompetitionRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;

    public StartCompetitionCommandHandler(ICompetitionRepository repo, ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<CompetitionDto> Handle(StartCompetitionCommand request, CancellationToken cancellationToken)
    {
        var competition = await _repo.GetByIdAsync(request.CompetitionId, cancellationToken)
                          ?? throw new Domain.Common.DomainException("Musobaqa topilmadi.");
        ResourceOwnership.EnsureCurrentLearner(_currentUser, competition.HostLearnerId);
        if (request.HostLearnerId != competition.HostLearnerId)
            throw new ForbiddenException("Only the competition host can start it.");

        competition.Start(request.HostLearnerId);

        await _repo.UpdateAsync(competition, cancellationToken);

        return CompetitionMapper.ToDto(competition);
    }
}
