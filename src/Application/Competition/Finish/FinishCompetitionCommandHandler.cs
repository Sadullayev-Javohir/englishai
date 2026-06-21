using Application.Competition.Dtos;
using Application.Competition.Ports;
using Application.Common;
using MediatR;

namespace Application.Competition.Finish;

public sealed class FinishCompetitionCommandHandler
    : IRequestHandler<FinishCompetitionCommand, CompetitionResultDto>
{
    private const int RewardPoints = 50;

    private readonly ICompetitionRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;

    public FinishCompetitionCommandHandler(ICompetitionRepository repo, ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<CompetitionResultDto> Handle(FinishCompetitionCommand request, CancellationToken cancellationToken)
    {
        var competition = await _repo.GetByIdAsync(request.CompetitionId, cancellationToken)
                          ?? throw new Domain.Common.DomainException("Musobaqa topilmadi.");
        ResourceOwnership.EnsureCurrentLearner(_currentUser, competition.HostLearnerId);
        if (request.HostLearnerId != competition.HostLearnerId)
            throw new ForbiddenException("Only the competition host can finish it.");

        competition.Finish();

        await _repo.UpdateAsync(competition, cancellationToken);

        var ranking = competition.Ranking()
            .Select((p, i) => CompetitionMapper.ToRankingRow(p, i + 1))
            .ToList();

        var winner = ranking.Count > 0 ? ranking[0] : null;

        return new CompetitionResultDto(
            competition.Id,
            competition.Title,
            ranking,
            winner?.ParticipantId ?? Guid.Empty,
            winner?.DisplayName ?? string.Empty,
            winner?.Score ?? 0,
            RewardPoints);
    }
}
