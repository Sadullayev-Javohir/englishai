using Application.Assistant.AskAssistant;
using Application.Assistant.Dtos;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Assistant;

public class AskAssistantQueryValidatorTests
{
    private readonly AskAssistantQueryValidator _validator = new();

    [Fact]
    public async Task Accepts_a_bounded_question_and_history()
    {
        var result = await _validator.ValidateAsync(new AskAssistantQuery(
            "Present Perfectni tushuntirib bering",
            new[] { new AssistantTurnDto("user", "Oldingi savol") }));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejects_an_empty_question(string question)
    {
        var result = await _validator.ValidateAsync(
            new AskAssistantQuery(question, Array.Empty<AssistantTurnDto>()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Rejects_invalid_history_role_and_excessive_history()
    {
        var turns = Enumerable.Range(1, AskAssistantQueryValidator.MaxHistoryTurns + 1)
            .Select(_ => new AssistantTurnDto("system", "text"))
            .ToArray();

        var result = await _validator.ValidateAsync(new AskAssistantQuery("Savol", turns));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "History");
        result.Errors.Should().Contain(error => error.PropertyName.EndsWith("Role"));
    }
}
