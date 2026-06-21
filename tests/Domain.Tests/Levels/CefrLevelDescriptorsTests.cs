using Domain.Assessment;
using Domain.Levels;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Levels;

public class CefrLevelDescriptorsTests
{
    [Theory]
    [InlineData(CefrLevel.A1)]
    [InlineData(CefrLevel.A2)]
    [InlineData(CefrLevel.B1)]
    [InlineData(CefrLevel.B2)]
    [InlineData(CefrLevel.C1)]
    [InlineData(CefrLevel.C2)]
    public void Every_level_has_one_can_do_for_each_communicative_skill(CefrLevel level)
    {
        var descriptors = CefrLevelDescriptors.For(level);

        descriptors.Should().HaveCount(CefrLevelDescriptors.DescribedSkills.Count);
        descriptors.Select(d => d.Skill).Should().Equal(CefrLevelDescriptors.DescribedSkills);
        descriptors.Should().OnlyContain(d => d.Level == level && !string.IsNullOrWhiteSpace(d.StatementEn));
    }

    [Fact]
    public void Statement_code_is_lowercase_level_and_skill()
    {
        var descriptor = CefrLevelDescriptors.For(CefrLevel.B1)
            .Single(d => d.Skill == Domain.Learning.SkillType.Speaking);

        descriptor.StatementCode.Should().Be("level.b1.speaking");
    }
}
