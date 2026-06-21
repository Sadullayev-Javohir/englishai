using Application.Speaking.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Speaking.GetRoleplayScenarios;

/// <summary>
/// Lists the roleplay scenarios the learner can choose from (for the scenario-picker screen). The
/// catalog is static curated content (20 scenarios per CEFR level, 120 total).
/// <para>
/// When <paramref name="Level"/> is set only that level's 20 scenarios are returned; otherwise
/// <paramref name="AllLevels"/> decides between the whole A1→C2 catalog (true) and - when both are
/// unset - the whole catalog as well. The picker passes the learner's chosen level filter.
/// </para>
/// </summary>
public sealed record GetRoleplayScenariosQuery(CefrLevel? Level = null, bool AllLevels = false)
    : IRequest<IReadOnlyList<RoleplayScenarioDto>>;
