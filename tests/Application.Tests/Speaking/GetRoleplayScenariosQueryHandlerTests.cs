using Application.Speaking.GetRoleplayScenarios;
using Domain.Assessment;
using Domain.Speaking;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Speaking;

public class GetRoleplayScenariosQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_the_full_curated_catalog_when_no_level_is_given()
    {
        var result = await new GetRoleplayScenariosQueryHandler()
            .Handle(new GetRoleplayScenariosQuery(), CancellationToken.None);

        result.Should().HaveCount(RoleplayScenarioCatalog.TotalScenarios);
        result.Select(s => s.Code).Should().Contain("job_interview");
    }

    [Fact]
    public async Task Handle_narrows_to_the_20_scenarios_of_a_pinned_level()
    {
        var result = await new GetRoleplayScenariosQueryHandler()
            .Handle(new GetRoleplayScenariosQuery(CefrLevel.B1), CancellationToken.None);

        result.Should().HaveCount(RoleplayScenarioCatalog.ScenariosPerLevel);
        result.Should().OnlyContain(s => s.Level == CefrLevel.B1);
    }

    [Fact]
    public async Task Handle_maps_the_image_id_so_the_picker_can_show_an_illustration()
    {
        var result = await new GetRoleplayScenariosQueryHandler()
            .Handle(new GetRoleplayScenariosQuery(CefrLevel.A1), CancellationToken.None);

        result.Should().OnlyContain(s => s.ImageId != Guid.Empty);
    }
}
