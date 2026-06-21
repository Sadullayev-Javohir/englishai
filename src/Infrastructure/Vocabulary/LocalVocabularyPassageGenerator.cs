using System.Text;
using Application.Vocabulary.Models;
using Application.Vocabulary.Ports;
using Domain.Assessment;

namespace Infrastructure.Vocabulary;

/// <summary>
/// Deterministic local stand-in for <see cref="IVocabularyPassageGenerator"/>, used when no LLM
/// key is configured so the vocabulary module runs and is testable offline (same gating pattern
/// as <c>LocalConversationTutor</c>). It weaves a short, CEFR-leveled passage around the topic
/// title using a vetted per-level word bank, and gives each target word an example sentence that
/// actually contains it - so hover translations and the cloze quiz both work. The real,
/// topic-specific content comes from <see cref="LlmVocabularyPassageGenerator"/> in production.
/// </summary>
public sealed class LocalVocabularyPassageGenerator : IVocabularyPassageGenerator
{
    public Task<GeneratedTopicContent> GenerateAsync(
        string title, CefrLevel level, int targetWordCount, CancellationToken cancellationToken = default)
    {
        var bank = WordBank(level);
        var words = new List<GeneratedTopicWord>(bank.Count);
        var passage = new StringBuilder();

        passage.Append("This short text is about ").Append(title).Append(". ");
        passage.Append("Read it carefully and notice how each new word is used in a sentence. ");

        foreach (var (word, uz) in bank.Take(targetWordCount))
        {
            var sentence = Sentence(title, word);
            passage.Append(sentence).Append(' ');
            words.Add(new GeneratedTopicWord(word, uz, sentence));
        }

        return Task.FromResult(new GeneratedTopicContent(passage.ToString().TrimEnd(), words));
    }

    // A sentence that always contains the target word, so the cloze quiz can blank it.
    private static string Sentence(string title, string word) =>
        $"When we talk about {title.ToLowerInvariant()}, the word {word} helps us explain our ideas clearly.";

    // Vetted, level-appropriate word banks (English + Uzbek). Offline dev/test content only;
    // production passages and words are generated per topic by the LLM (rules 10, 11).
    private static IReadOnlyList<(string Word, string Uz)> WordBank(CefrLevel level) => level switch
    {
        CefrLevel.A1 => A1,
        CefrLevel.A2 => A2,
        CefrLevel.B1 => B1,
        CefrLevel.B2 => B2,
        CefrLevel.C1 => C1,
        _ => C2,
    };

    private static readonly (string, string)[] A1 =
    {
        ("family", "oila"), ("happy", "baxtli"), ("water", "suv"), ("friend", "do'st"),
        ("morning", "ertalab"), ("school", "maktab"), ("colour", "rang"), ("animal", "hayvon"),
        ("food", "ovqat"), ("house", "uy"), ("play", "o'ynamoq"), ("small", "kichik"),
        ("number", "raqam"), ("clean", "toza"), ("warm", "iliq"),
        ("garden", "bog'"), ("family", "oila"), ("music", "musiqa"), ("today", "bugun"),
        ("learn", "o'rganmoq"),
    };

    private static readonly (string, string)[] A2 =
    {
        ("travel", "sayohat"), ("healthy", "sog'lom"), ("cheap", "arzon"), ("journey", "safar"),
        ("weather", "ob-havo"), ("hobby", "mashg'ulot"), ("ticket", "chipta"), ("market", "bozor"),
        ("careful", "ehtiyotkor"), ("message", "xabar"), ("decide", "qaror qilmoq"), ("busy", "band"),
        ("repair", "ta'mirlamoq"), ("delicious", "mazali"), ("nearby", "yaqin"),
        ("invite", "taklif qilmoq"), ("prepare", "tayyorlamoq"), ("comfortable", "qulay"),
        ("direction", "yo'nalish"), ("available", "mavjud"),
    };

    private static readonly (string, string)[] B1 =
    {
        ("experience", "tajriba"), ("opinion", "fikr"), ("improve", "yaxshilamoq"), ("environment", "atrof-muhit"),
        ("culture", "madaniyat"), ("career", "karyera"), ("benefit", "foyda"), ("achieve", "erishmoq"),
        ("balance", "muvozanat"), ("local", "mahalliy"), ("reduce", "kamaytirmoq"), ("communication", "muloqot"),
        ("responsible", "mas'uliyatli"), ("budget", "byudjet"), ("community", "jamoa"),
        ("challenge", "qiyinchilik"), ("develop", "rivojlantirmoq"), ("solution", "yechim"),
        ("opportunity", "imkoniyat"), ("habit", "odat"),
    };

    private static readonly (string, string)[] B2 =
    {
        ("inequality", "tengsizlik"), ("sustainable", "barqaror"), ("innovation", "innovatsiya"), ("influence", "ta'sir"),
        ("consequence", "oqibat"), ("attitude", "munosabat"), ("efficient", "samarali"), ("perspective", "nuqtai nazar"),
        ("significant", "muhim"), ("approach", "yondashuv"), ("contribute", "hissa qo'shmoq"), ("awareness", "xabardorlik"),
        ("flexible", "moslashuvchan"), ("priority", "ustuvorlik"), ("genuine", "haqiqiy"),
        ("evaluate", "baholamoq"), ("complex", "murakkab"), ("evidence", "dalil"),
        ("strategy", "strategiya"), ("reliable", "ishonchli"),
    };

    private static readonly (string, string)[] C1 =
    {
        ("accountability", "javobgarlik"), ("ambiguous", "ikki ma'noli"), ("coherent", "izchil"), ("framework", "asos"),
        ("implication", "oqibat"), ("inherent", "tabiiy xos"), ("nuance", "nozik farq"), ("paradigm", "paradigma"),
        ("plausible", "ishonarli"), ("prevailing", "hukmron"), ("rigorous", "qat'iy"), ("subtle", "nozik"),
        ("undermine", "putur yetkazmoq"), ("viable", "hayotiy"), ("comprehensive", "keng qamrovli"),
        ("articulate", "aniq ifodalamoq"), ("conventional", "an'anaviy"), ("empirical", "tajribaviy"),
        ("facilitate", "yengillashtirmoq"), ("resilient", "bardoshli"),
    };

    private static readonly (string, string)[] C2 =
    {
        ("epistemic", "bilimga oid"), ("hegemony", "hukmronlik"), ("paradoxical", "paradoksal"), ("dichotomy", "qarama-qarshilik"),
        ("ostensibly", "go'yoki"), ("contingent", "shartli"), ("normative", "me'yoriy"), ("salient", "yaqqol"),
        ("intractable", "yechilmas"), ("juxtapose", "yonma-yon qo'ymoq"), ("ubiquitous", "hamma joyda mavjud"), ("nuanced", "nozik"),
        ("predicated", "asoslangan"), ("antithetical", "zid"), ("emergent", "yuzaga keluvchi"),
        ("dialectical", "dialektik"), ("idiosyncratic", "o'ziga xos"), ("meticulous", "sinchkov"),
        ("reconcile", "murosaga keltirmoq"), ("tenuous", "zaif asoslangan"),
    };
}
