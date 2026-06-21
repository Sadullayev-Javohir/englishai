using System.Text;
using Application.Books.Models;
using Application.Books.Ports;
using Domain.Assessment;
using Domain.Books;

namespace Infrastructure.Books;

/// <summary>
/// Deterministic local stand-in for <see cref="IBookContentGenerator"/>, used when no LLM key is
/// configured so the Books module runs and is testable offline (same gating pattern as the other
/// Local generators). It weaves a short, CEFR-leveled English section around the book and section
/// titles using a vetted per-level word bank and produces exactly
/// <see cref="BookSection.QuestionsPerSection"/> comprehension questions answerable from the text -
/// one main-idea question plus nine vocabulary checks, each with an English explanation. Real
/// story-specific content comes from <see cref="LlmBookContentGenerator"/> in production.
/// </summary>
public sealed class LocalBookContentGenerator : IBookContentGenerator
{
    public Task<GeneratedBookSection> GenerateAsync(
        string bookTitle,
        string synopsis,
        string sectionTitle,
        int sectionNumber,
        int totalSections,
        CefrLevel level,
        CancellationToken cancellationToken = default)
    {
        // Nine words drive the nine vocabulary questions; the tenth question is main-idea.
        var bank = WordBank(level).Take(9).ToList();
        var lowerSection = sectionTitle.ToLowerInvariant();

        var body = new StringBuilder();
        body.Append("This is section ").Append(sectionNumber).Append(" of ").Append(totalSections)
            .Append(" of the book \"").Append(bookTitle).Append("\", titled \"").Append(sectionTitle).Append("\". ");
        body.Append("In this part of the story, read carefully and notice how each useful word is used. ");
        foreach (var (word, _) in bank)
            body.Append(Sentence(lowerSection, word)).Append(' ');
        body.Append("When you finish reading, answer the ten questions to show that you understood this section.");

        var questions = BuildQuestions(sectionTitle, bank);

        return Task.FromResult(new GeneratedBookSection(body.ToString().Trim(), questions));
    }

    private static string Sentence(string lowerSection, string word) =>
        $"As the chapter about {lowerSection} continues, the word \"{word}\" helps describe what happens.";

    // One main-idea question plus nine vocabulary questions = exactly ten.
    private static IReadOnlyList<GeneratedBookQuestion> BuildQuestions(
        string sectionTitle, IReadOnlyList<(string Word, string Uz)> bank)
    {
        var questions = new List<GeneratedBookQuestion>
        {
            new(
                "What is this section mainly about?",
                new List<string> { sectionTitle, "A cooking recipe", "A football match", "A weather report" },
                0,
                $"The section is titled \"{sectionTitle}\" and the text is about it, so that is its main idea."),
        };

        foreach (var (word, uz) in bank)
        {
            var options = BuildOptions(word, bank);
            questions.Add(new GeneratedBookQuestion(
                $"Which English word from the text means \"{uz}\"?",
                options,
                options.IndexOf(word),
                $"In the section, \"{word}\" is used with the meaning \"{uz}\"."));
        }

        return questions;
    }

    // Four distinct options including the correct word, drawn deterministically from the bank.
    private static List<string> BuildOptions(string answer, IReadOnlyList<(string Word, string Uz)> bank)
    {
        var options = new List<string> { answer };
        foreach (var (word, _) in bank)
        {
            if (options.Count >= 4)
                break;
            if (!options.Contains(word))
                options.Add(word);
        }

        return options;
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
        ("night", "tun"), ("garden", "bog'"), ("river", "daryo"), ("village", "qishloq"),
    };

    private static readonly (string Word, string Uz)[] A2 =
    {
        ("travel", "sayohat"), ("healthy", "sog'lom"), ("weather", "ob-havo"), ("market", "bozor"),
        ("ticket", "chipta"), ("busy", "band"), ("decide", "qaror qilmoq"), ("nearby", "yaqin"),
        ("journey", "yo'l"), ("memory", "xotira"), ("promise", "va'da"), ("danger", "xavf"),
    };

    private static readonly (string Word, string Uz)[] B1 =
    {
        ("experience", "tajriba"), ("opinion", "fikr"), ("improve", "yaxshilamoq"), ("culture", "madaniyat"),
        ("benefit", "foyda"), ("achieve", "erishmoq"), ("community", "jamoa"), ("reduce", "kamaytirmoq"),
        ("courage", "jasorat"), ("discover", "kashf qilmoq"), ("mystery", "sir"), ("decision", "qaror"),
    };

    private static readonly (string Word, string Uz)[] B2 =
    {
        ("inequality", "tengsizlik"), ("sustainable", "barqaror"), ("influence", "ta'sir"), ("attitude", "munosabat"),
        ("efficient", "samarali"), ("approach", "yondashuv"), ("awareness", "xabardorlik"), ("priority", "ustuvorlik"),
        ("ambition", "intilish"), ("betrayal", "xiyonat"), ("resilience", "chidamlilik"), ("consequence", "oqibat"),
    };

    private static readonly (string Word, string Uz)[] C1 =
    {
        ("accountability", "javobgarlik"), ("coherent", "izchil"), ("framework", "asos"), ("implication", "oqibat"),
        ("nuance", "nozik farq"), ("plausible", "ishonarli"), ("rigorous", "qat'iy"), ("viable", "hayotiy"),
        ("ambiguity", "noaniqlik"), ("perception", "idrok"), ("inevitable", "muqarrar"), ("profound", "chuqur"),
    };

    private static readonly (string Word, string Uz)[] C2 =
    {
        ("hegemony", "hukmronlik"), ("dichotomy", "qarama-qarshilik"), ("normative", "me'yoriy"), ("salient", "yaqqol"),
        ("ubiquitous", "hamma joyda"), ("contingent", "shartli"), ("emergent", "yuzaga keluvchi"), ("nuanced", "nozik"),
        ("paradigm", "andoza"), ("juxtaposition", "yonma-yonlik"), ("ephemeral", "o'tkinchi"), ("intrinsic", "ichki"),
    };
}
