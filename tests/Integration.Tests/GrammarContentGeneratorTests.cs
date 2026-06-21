using Application.Grammar.Models;
using Application.Grammar.Ports;
using Domain.Assessment;
using Domain.Grammar;
using Infrastructure.Grammar;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Verifies the topic-scoped grammar generators: the offline Local generator produces a complete,
/// auto-gradable 5-step lesson for every CEFR level, and the Claude generator's parser reads valid
/// JSON and rejects malformed payloads (so a bad LLM reply leaves the lesson honestly pending).
/// </summary>
public class GrammarContentGeneratorTests
{
    private static readonly CefrLevel[] Levels =
        { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 };

    [Fact]
    public async Task LocalGenerator_produces_a_complete_lesson_for_each_level()
    {
        var generator = new LocalGrammarContentGenerator();

        foreach (var level in Levels)
        {
            var content = await generator.GenerateAsync("The Museum", "articles", level);

            content.HasContent.Should().BeTrue();
            content.ContextIntro.Should().NotBeNullOrWhiteSpace();
            content.RuleExplanation.Should().NotBeNullOrWhiteSpace();
            content.Exercises.Should().HaveCount(10);
            content.ApplicationTasks.Should().Contain(t => t.TargetSkill == Domain.Learning.SkillType.Speaking);
            content.ApplicationTasks.Should().Contain(t => t.TargetSkill == Domain.Learning.SkillType.Writing);

            // Every exercise is answerable: the correct index is in range and points at a real option.
            content.Exercises.Should().OnlyContain(e =>
                e.Options.Count >= 2 &&
                e.CorrectOptionIndex >= 0 &&
                e.CorrectOptionIndex < e.Options.Count);

            // The generated content survives the domain invariants (the handler builds these).
            var lesson = GrammarLesson.ForTopic(
                Guid.NewGuid(), "The Museum", Domain.Learning.ErrorCategory.Articles, level, "articles",
                DateTimeOffset.UtcNow);
            var exercises = content.Exercises
                .Select(e => GrammarExercise.Create(e.Type, e.Prompt, e.Options, e.CorrectOptionIndex, null, e.Explanation))
                .ToList();
            var tasks = content.ApplicationTasks
                .Select(t => GrammarApplicationTask.Create(t.TargetSkill, t.Prompt))
                .ToList();
            var examples = content.Examples
                .Select(e => GrammarExample.Create(e.English, e.Uzbek))
                .ToList();
            var mistakes = content.CommonMistakesUz
                .Select(m => GrammarCommonMistake.Create(m))
                .ToList();
            var fill = () => lesson.FillContent(content.ContextIntro, content.RuleExplanation, examples, mistakes, exercises, tasks);
            fill.Should().NotThrow();
        }
    }

    [Fact]
    public void ClaudeParser_reads_valid_json_and_rejects_garbage()
    {
        var content = JsonGrammarContentProvider.FromEmbeddedResource();

        const string json =
            """
            Sure: {"intro":"At the museum we use 'the' for a specific place.",
              "rule":"Use 'the' for a specific museum and 'a' for any museum.",
              "exercises":[{"type":"recognition","q":"Which is correct?","options":["I saw a Mona Lisa","I saw the Mona Lisa"],"answer":1,"why":"The Mona Lisa is a specific painting."}],
              "tasks":[{"skill":"speaking","prompt":"Describe the museum."},{"skill":"writing","prompt":"Write about the museum."}]}
            """;

        var parsed = LlmGrammarContentGenerator.Parse(json, content);

        parsed.HasContent.Should().BeTrue();
        parsed.Exercises.Should().ContainSingle(e =>
            e.Type == GrammarExerciseType.Recognition && e.CorrectOptionIndex == 1 && e.Explanation != null);
        parsed.ApplicationTasks.Should().HaveCount(2);

        LlmGrammarContentGenerator.Parse("not json at all", content).Should().Be(GeneratedGrammarContent.Empty);
        // A lesson with no exercises is unusable, so it parses to Empty (lesson stays pending).
        LlmGrammarContentGenerator.Parse("{\"intro\":\"x\",\"rule\":\"y\",\"exercises\":[]}", content).HasContent.Should().BeFalse();
    }

    [Fact]
    public void ClaudeParser_resolves_known_mistake_codes_and_drops_free_text_or_unknown_codes()
    {
        // The LLM must return fixed CODES for "mistakes" (docs/development-guide.md rule 11) - never write the
        // Uzbek explanation itself. The real embedded provider resolves a known code to its vetted
        // Uzbek template, and silently drops anything that isn't a real code (an unknown code, or
        // free-form prose the model wrote instead of following the contract) rather than ever
        // surfacing raw model text to the learner.
        var content = JsonGrammarContentProvider.FromEmbeddedResource();

        const string json =
            """
            {"intro":"We use articles when talking about the museum.",
              "rule":"Use 'the' for a specific museum and 'a' for any museum.",
              "exercises":[{"type":"recognition","q":"Which is correct?","options":["I saw a Mona Lisa","I saw the Mona Lisa"],"answer":1,"why":"It is a specific painting."}],
              "mistakes":["missing_article","not_a_real_code","O'zbek tilida artikl yo'q shuning uchun tez-tez unutiladi."]}
            """;

        var parsed = LlmGrammarContentGenerator.Parse(json, content);

        parsed.HasContent.Should().BeTrue();
        parsed.CommonMistakesUz.Should().ContainSingle();
        parsed.CommonMistakesUz.Should().Contain(content.GetMistakeExplanation("missing_article"));
        // The unknown code and the free-form Uzbek prose must never appear in the output.
        parsed.CommonMistakesUz.Should().NotContain("not_a_real_code");
        parsed.CommonMistakesUz.Should().NotContain("O'zbek tilida artikl yo'q shuning uchun tez-tez unutiladi.");
    }

    [Fact]
    public void ClaudeParser_drops_degenerate_example_translations()
    {
        // examples[].uz is a dynamic translation of the model's own English sentence, so it can't be
        // a fixed template lookup - instead a heuristic second-pass guard rejects the classic
        // degenerate outputs (empty, an exact copy of the English, or implausibly short).
        var content = JsonGrammarContentProvider.FromEmbeddedResource();

        const string json =
            """
            {"intro":"We use articles when talking about the museum.",
              "rule":"Use 'the' for a specific museum and 'a' for any museum.",
              "exercises":[{"type":"recognition","q":"Which is correct?","options":["I saw a Mona Lisa","I saw the Mona Lisa"],"answer":1,"why":"It is a specific painting."}],
              "examples":[
                {"en":"She is reading a book about the museum.","uz":"U muzey haqidagi kitobni o'qiyapti."},
                {"en":"He visited the museum yesterday.","uz":"He visited the museum yesterday."},
                {"en":"They enjoyed the exhibition very much.","uz":"U"},
                {"en":"We are planning a trip.","uz":""}
              ]}
            """;

        var parsed = LlmGrammarContentGenerator.Parse(json, content);

        parsed.Examples.Should().ContainSingle();
        parsed.Examples.Single().English.Should().Be("She is reading a book about the museum.");
        parsed.Examples.Single().Uzbek.Should().Be("U muzey haqidagi kitobni o'qiyapti.");
    }
}
