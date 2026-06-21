using Application.Speaking.Dtos;
using Domain.Speaking;
using MediatR;

namespace Application.Speaking.GetRoleplayScenarios;

public sealed class GetRoleplayScenariosQueryHandler
    : IRequestHandler<GetRoleplayScenariosQuery, IReadOnlyList<RoleplayScenarioDto>>
{
    public Task<IReadOnlyList<RoleplayScenarioDto>> Handle(
        GetRoleplayScenariosQuery request, CancellationToken cancellationToken)
    {
        // A specific level narrows to its 20 scenarios; otherwise the whole A1→C2 catalog is
        // returned (the "all levels" browse view, or when no level filter was supplied).
        var definitions = request.Level is { } level
            ? RoleplayScenarioCatalog.ForLevel(level)
            : RoleplayScenarioCatalog.All;

        IReadOnlyList<RoleplayScenarioDto> scenarios = definitions
            .Select(RoleplayScenarioDto.FromDomain)
            .ToList();

        return Task.FromResult(scenarios);
    }
}
