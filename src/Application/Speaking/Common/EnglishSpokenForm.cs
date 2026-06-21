using System.Globalization;
using System.Text.RegularExpressions;
using Domain.Speaking;

namespace Application.Speaking.Common;

public sealed record SpokenFormToken(string Original, string Spoken, IReadOnlyList<string> SpokenWords);

public sealed record SpokenFormText(string Original, string Spoken, IReadOnlyList<SpokenFormToken> Tokens)
{
    public PronunciationResult MapResult(PronunciationResult result)
    {
        if (Tokens.Count == 0 || result.Words.Count == 0)
            return result;

        var mapped = new List<WordPronunciation>();
        var tokenIndex = 0;
        for (var resultIndex = 0; resultIndex < result.Words.Count; resultIndex++)
        {
            var word = result.Words[resultIndex];
            if (tokenIndex >= Tokens.Count)
            {
                mapped.Add(word);
                continue;
            }

            var token = Tokens[tokenIndex];
            if (!SpokenWordMatches(word.Word, token.SpokenWords[0]))
            {
                mapped.Add(word);
                continue;
            }

            var consumed = new List<WordPronunciation> { word };
            while (consumed.Count < token.SpokenWords.Count)
            {
                var nextIndex = resultIndex + consumed.Count;
                if (nextIndex >= result.Words.Count)
                    break;
                if (!SpokenWordMatches(result.Words[nextIndex].Word, token.SpokenWords[consumed.Count]))
                    break;
                consumed.Add(result.Words[nextIndex]);
            }

            if (consumed.Count != token.SpokenWords.Count)
            {
                mapped.Add(word);
                continue;
            }

            var weakest = consumed.MinBy(item => item.AccuracyScore)!;
            mapped.Add(new WordPronunciation(
                token.Original,
                weakest.AccuracyScore,
                consumed.Select(item => item.ErrorType).FirstOrDefault(type => type != PronunciationErrorType.None),
                consumed.SelectMany(item => item.Phonemes).ToList(),
                token.Spoken));
            resultIndex += consumed.Count - 1;
            tokenIndex++;
        }

        return new PronunciationResult(
            result.OverallScore,
            result.AccuracyScore,
            result.FluencyScore,
            result.CompletenessScore,
            mapped,
            result.IsAuthentic);
    }

    private static string NormalizeWord(string value) =>
        Regex.Replace(value, "[^a-z']", string.Empty, RegexOptions.IgnoreCase);

    private static bool SpokenWordMatches(string actual, string expected)
    {
        var normalizedActual = NormalizeWord(actual);
        var normalizedExpected = NormalizeWord(expected);
        if (string.Equals(normalizedActual, normalizedExpected, StringComparison.OrdinalIgnoreCase))
            return true;
        return string.Equals(
            normalizedActual.Replace("'", string.Empty),
            normalizedExpected.Replace("'", string.Empty),
            StringComparison.OrdinalIgnoreCase);
    }
}

public static partial class EnglishSpokenForm
{
    private static readonly string[] Ones =
        { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };

    private static readonly string[] Tens =
        { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

    public static SpokenFormText NormalizeText(string text)
    {
        var tokens = new List<SpokenFormToken>();
        var spoken = NumericTokenRegex().Replace(text, match =>
        {
            var value = ToSpoken(match.Value);
            if (value is null)
                return match.Value;
            tokens.Add(new SpokenFormToken(match.Value, value, SplitWords(value)));
            return value;
        });
        return new SpokenFormText(text, spoken, tokens);
    }

    public static string? ToSpoken(string value)
    {
        var trimmed = value.Trim();
        var time = TimeRegex().Match(trimmed);
        if (time.Success)
        {
            var hour = int.Parse(time.Groups["hour"].Value, CultureInfo.InvariantCulture);
            var minute = int.Parse(time.Groups["minute"].Value, CultureInfo.InvariantCulture);
            if (hour is > 23 || minute is > 59)
                return null;
            var normalizedHour = hour == 0 ? 12 : hour > 12 ? hour - 12 : hour;
            var spokenHour = IntegerToWords(normalizedHour);
            var spokenMinute = minute == 0 && time.Groups["period"].Success
                ? string.Empty
                : minute == 0
                    ? "o'clock"
                    : minute < 10
                        ? $"oh {Ones[minute]}"
                        : IntegerToWords(minute);
            var period = time.Groups["period"].Success ? $" {time.Groups["period"].Value.ToUpperInvariant()}" : string.Empty;
            return $"{spokenHour} {spokenMinute}{period}".Replace("  ", " ").Trim();
        }

        var decimalMatch = DecimalRegex().Match(trimmed);
        if (decimalMatch.Success)
        {
            var whole = long.Parse(decimalMatch.Groups["whole"].Value, CultureInfo.InvariantCulture);
            var fraction = string.Join(" ", decimalMatch.Groups["fraction"].Value.Select(character => Ones[character - '0']));
            return $"{IntegerToWords(whole)} point {fraction}";
        }

        if (!long.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var integer) || integer > 999_999_999)
            return null;
        return integer is >= 2000 and <= 2099
            ? integer == 2000 ? "two thousand" : $"twenty {IntegerToWords(integer - 2000)}"
            : IntegerToWords(integer);
    }

    private static IReadOnlyList<string> SplitWords(string value) =>
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(word => word.Split('-', StringSplitOptions.RemoveEmptyEntries))
            .Select(word => word.Trim().ToLowerInvariant())
            .ToList();

    private static string IntegerToWords(long value)
    {
        if (value < 20)
            return Ones[value];
        if (value < 100)
            return value % 10 == 0 ? Tens[value / 10] : $"{Tens[value / 10]}-{Ones[value % 10]}";
        if (value < 1_000)
            return value % 100 == 0 ? $"{Ones[value / 100]} hundred" : $"{Ones[value / 100]} hundred {IntegerToWords(value % 100)}";
        if (value < 1_000_000)
            return JoinScale(value, 1_000, "thousand");
        return JoinScale(value, 1_000_000, "million");
    }

    private static string JoinScale(long value, long scale, string label) =>
        value % scale == 0
            ? $"{IntegerToWords(value / scale)} {label}"
            : $"{IntegerToWords(value / scale)} {label} {IntegerToWords(value % scale)}";

    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?:\d{1,2}:\d{2}(?:\s?(?:AM|PM|am|pm))?|\d+\.\d+|\d+)(?![\p{L}\p{N}])")]
    private static partial Regex NumericTokenRegex();

    [GeneratedRegex(@"^(?<hour>\d{1,2}):(?<minute>\d{2})(?:\s?(?<period>AM|PM))?$", RegexOptions.IgnoreCase)]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"^(?<whole>\d+)\.(?<fraction>\d+)$")]
    private static partial Regex DecimalRegex();
}
