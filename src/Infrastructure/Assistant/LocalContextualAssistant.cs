using Application.Assistant.Ports;

namespace Infrastructure.Assistant;

public sealed class LocalContextualAssistant : IContextualAssistant
{
    public Task<string?> AnswerAsync(string area, string title, string context, string focusText,
        string question, IReadOnlyList<ContextualAssistantTurn> history, CancellationToken cancellationToken = default)
    {
        if (area.Trim().Equals("vocabulary", StringComparison.OrdinalIgnoreCase)
            && TryVocabularyAnswer(context, question, out var vocabularyAnswer))
            return Task.FromResult<string?>(vocabularyAnswer);

        var scope = string.IsNullOrWhiteSpace(title) ? "shu dars" : title.Trim();
        var focus = string.IsNullOrWhiteSpace(focusText) ? string.Empty : $"\n\nTanlangan qism: **{Trim(focusText, 220)}**";
        var answer = area.Trim().ToLowerInvariant() switch
        {
            "vocabulary" => $"## {scope}\nBu savol uchun darsdagi soz va misollar asosiy manba hisoblanadi.{focus}\n\nSozni tarjimasi, soz turkumi va gapdagi vazifasi bilan birga organing. Keyin uni oz gapingizda ishlatib koring.",
            "grammar" => $"## {scope}\nDarsdagi qoida va formulani asos qilib oling.{focus}\n\nAvval tuzilishni aniqlang, keyin qachon ishlatilishini tekshiring va darsdagi misolga oxshash yangi gap tuzing.",
            "reading" => $"## {scope}\nJavob matndagi dalilga tayanishi kerak.{focus}\n\nAsosiy fikrni toping, savoldagi kalit sozlarni matndan qidiring va javobni shu jumla bilan izohlang.",
            "writing" => $"## {scope}\nTopshiriq talabiga mos reja tuzing.{focus}\n\nKirish, asosiy fikrlar va xulosani ajrating. Har bir fikrga aniq misol qoshing va oxirida grammatika hamda soz sonini tekshiring.",
            "speaking" => $"## {scope}\nQisqa va tabiiy javob bering.{focus}\n\nFikr, sabab va bitta misol tartibidan foydalaning. Gaplarni baland ovozda aytib, talaffuz va fe'l zamonini tekshiring.",
            "listening" => $"## {scope}\nJavob transcriptdagi aniq iboraga tayanishi kerak.{focus}\n\nKalit sozlarni, fe'l zamonini va boglovchi sozlarni tinglang. Tushunmagan qismni kichik bolaklarga ajratib qayta oqing.",
            "video" => $"## {scope}\nVideo transcripti va joriy jumla asosiy manba hisoblanadi.{focus}\n\nJumladagi kalit soz, grammatika va umumiy mavzuni birga tahlil qiling.",
            "books" => $"## {scope}\nJavob kitobning joriy bolimi va synopsisiga tayanishi kerak.{focus}\n\nVoqea, qahramon yoki iborani matndagi dalil bilan tushuntiring. Javobi berilmagan quiz savollarining tayyor javobini oshkor qilmang.",
            _ => "Men ingliz tili boyicha soz, grammatika, reading, writing, speaking va listening savollarida yordam beraman. Savolingizdagi asosiy soz yoki gapni yuboring.",
        };
        return Task.FromResult<string?>(answer);
    }

    private static bool TryVocabularyAnswer(string context, string question, out string answer)
    {
        answer = string.Empty;
        var requested = string.Join(' ', question.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Trim('?', '.', ',', '!', '\'', '"'))
            .FirstOrDefault(word => word.Length > 2
                                    && !VocabularyStopWords.Contains(word)
                                    && context.Split('\n').Any(value => value.TrimStart().StartsWith($"- {word} |", StringComparison.OrdinalIgnoreCase)))
            ;
        if (string.IsNullOrWhiteSpace(requested)) return false;
        var line = context.Split('\n').FirstOrDefault(value => value.TrimStart().StartsWith($"- {requested} |", StringComparison.OrdinalIgnoreCase));
        if (line is null) return false;
        var parts = line.TrimStart('-', ' ').Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;
        var partOfSpeech = parts.FirstOrDefault(x => x.StartsWith("part of speech:", StringComparison.OrdinalIgnoreCase))?.Split(':', 2)[1].Trim() ?? "not provided";
        var example = AfterColon(parts.FirstOrDefault(x => x.StartsWith("example:", StringComparison.OrdinalIgnoreCase)));
        var usage = AfterColon(parts.FirstOrDefault(x => x.StartsWith("usage:", StringComparison.OrdinalIgnoreCase)));
        answer = $"## {parts[0]}\n- **Tarjima:** {parts[1]}\n- **So'z turkumi:** {partOfSpeech}\n- **Qayerda ishlatiladi:** {(string.IsNullOrWhiteSpace(usage) || usage == "not provided" ? "Odam, narsa yoki tushunchani gapda aniq ifodalash uchun kontekstga mos ishlatiladi." : usage)}\n- **Misol:** {(string.IsNullOrWhiteSpace(example) || example == "not provided" ? $"I used the word '{parts[0]}' in a clear sentence." : example)}";
        return true;
    }

    private static readonly HashSet<string> VocabularyStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "what", "does", "mean", "about", "word", "please", "nima", "qanday", "haqida", "tushuntir", "ber"
    };

    private static string? AfterColon(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var index = value.IndexOf(':');
        return index < 0 || index == value.Length - 1 ? null : value[(index + 1)..].Trim();
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max] + "...";
}
