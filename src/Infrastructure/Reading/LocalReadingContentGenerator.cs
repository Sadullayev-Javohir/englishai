using System.Text;
using Application.Reading.Models;
using Application.Reading.Ports;
using Domain.Assessment;

namespace Infrastructure.Reading;

/// <summary>
/// Deterministic local stand-in for <see cref="IReadingContentGenerator"/>, used when no LLM key
/// is configured so the Reading module runs and is testable offline (same gating pattern as
/// <c>LocalVocabularyPassageGenerator</c>). It weaves a short, CEFR-leveled English passage around
/// the topic title using a vetted per-level word bank, builds an interactive glossary from those
/// words and three comprehension questions answerable from the text - each with an English
/// explanation (the immersion teaching note). Real topic-specific content comes from
/// <see cref="LlmReadingContentGenerator"/> in production.
/// </summary>
public sealed class LocalReadingContentGenerator : IReadingContentGenerator
{
    private const int GlossaryWords = 6;

    public Task<GeneratedReadingContent> GenerateAsync(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default)
    {
        var bank = WordBank(level).Take(GlossaryWords).ToList();
        var lower = title.ToLowerInvariant();

        var body = new StringBuilder();
        body.Append("This short text is about ").Append(title).Append(". ");
        body.Append("Read it carefully and notice how each useful word is used in a sentence. ");
        foreach (var (word, _) in bank)
            body.Append(Sentence(lower, word)).Append(' ');
        body.Append("Now answer the questions to check that you understood the text.");

        var glossary = bank
            .Select(b => new GeneratedReadingGlossary(b.Word, b.Uz, Sentence(lower, b.Word)))
            .ToList();

        var questions = BuildQuestions(title, bank);

        return Task.FromResult(new GeneratedReadingContent(body.ToString().Trim(), glossary, questions));
    }

    private static string Sentence(string lowerTitle, string word) =>
        $"When we read about {lowerTitle}, the word \"{word}\" helps us describe what happens clearly.";

    // Three deterministic, text-answerable questions: one main-idea and two vocabulary checks.
    private static IReadOnlyList<GeneratedReadingQuestion> BuildQuestions(
        string title, IReadOnlyList<(string Word, string Uz)> bank)
    {
        var questions = new List<GeneratedReadingQuestion>();

        var mainOptions = new List<string> { title, "A football match", "A cooking recipe", "A space mission" };
        questions.Add(new GeneratedReadingQuestion(
            "What is this text mainly about?",
            mainOptions,
            0,
            $"The text introduces and describes \"{title}\", so that is its main idea."));

        // Two vocabulary questions: pick the English word that matches a given Uzbek meaning.
        for (var i = 0; i < 2 && i < bank.Count; i++)
        {
            var answer = bank[i].Word;
            var options = bank.Select(b => b.Word).Distinct().Take(4).ToList();
            if (!options.Contains(answer))
                options[0] = answer;
            var correct = options.IndexOf(answer);

            questions.Add(new GeneratedReadingQuestion(
                $"Which English word means \"{bank[i].Uz}\"?",
                options,
                correct,
                $"In the text, \"{answer}\" is used with the meaning \"{bank[i].Uz}\"."));
        }

        return questions;
    }

    private static IReadOnlyList<(string Word, string Uz)> WordBank(CefrLevel level) => level switch
    {
        CefrLevel.A1 => A1,
        CefrLevel.A2 => A2,
        CefrLevel.B1 => B1,
        CefrLevel.B2 => B2,
        CefrLevel.C1 => C1,
        _ => C2,
    };

    private static readonly (string Word, string Uz)[] A1 =
    {
        ("family", "oila"), ("happy", "baxtli"), ("morning", "ertalab"), ("friend", "do'st"),
        ("water", "suv"), ("house", "uy"), ("school", "maktab"), ("food", "ovqat"),
    };

    private static readonly (string Word, string Uz)[] A2 =
    {
        ("travel", "sayohat"), ("healthy", "sog'lom"), ("weather", "ob-havo"), ("market", "bozor"),
        ("ticket", "chipta"), ("busy", "band"), ("decide", "qaror qilmoq"), ("nearby", "yaqin"),
    };

    private static readonly (string Word, string Uz)[] B1 =
    {
        ("experience", "tajriba"), ("opinion", "fikr"), ("improve", "yaxshilamoq"), ("culture", "madaniyat"),
        ("benefit", "foyda"), ("achieve", "erishmoq"), ("community", "jamoa"), ("reduce", "kamaytirmoq"),
    };

    private static readonly (string Word, string Uz)[] B2 =
    {
        ("inequality", "tengsizlik"), ("sustainable", "barqaror"), ("influence", "ta'sir"), ("attitude", "munosabat"),
        ("efficient", "samarali"), ("approach", "yondashuv"), ("awareness", "xabardorlik"), ("priority", "ustuvorlik"),
    };

    private static readonly (string Word, string Uz)[] C1 =
    {
        ("accountability", "javobgarlik"), ("coherent", "izchil"), ("framework", "asos"), ("implication", "oqibat"),
        ("nuance", "nozik farq"), ("plausible", "ishonarli"), ("rigorous", "qat'iy"), ("viable", "hayotiy"),
    };

    private static readonly (string Word, string Uz)[] C2 =
    {
        ("hegemony", "hukmronlik"), ("dichotomy", "qarama-qarshilik"), ("normative", "me'yoriy"), ("salient", "yaqqol"),
        ("ubiquitous", "hamma joyda"), ("contingent", "shartli"), ("emergent", "yuzaga keluvchi"), ("nuanced", "nozik"),
    };
}
