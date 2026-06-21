using Application.Competition.Dtos;
using Application.Competition.Ports;
using MediatR;

namespace Application.Competition.Join;

public sealed class JoinCompetitionCommandHandler
    : IRequestHandler<JoinCompetitionCommand, CompetitionDto>
{
    private readonly ICompetitionRepository _repo;

    public JoinCompetitionCommandHandler(ICompetitionRepository repo)
    {
        _repo = repo;
    }

    public async Task<CompetitionDto> Handle(JoinCompetitionCommand request, CancellationToken cancellationToken)
    {
        var competition = await _repo.GetByIdAsync(request.CompetitionId, cancellationToken)
                          ?? throw new Domain.Common.DomainException("Musobaqa topilmadi.");

        competition.Join(request.LearnerId, request.DisplayName, request.AccessCode);

        var accessCode = request.LearnerId == competition.HostLearnerId ? competition.AccessCode : null;

        await _repo.UpdateAsync(competition, cancellationToken);

        return CompetitionMapper.ToDto(competition, accessCode);
    }
}
