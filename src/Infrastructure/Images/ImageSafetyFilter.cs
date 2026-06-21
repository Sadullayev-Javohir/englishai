using System.Text.RegularExpressions;

namespace Infrastructure.Images;

internal static partial class ImageSafetyFilter
{
    public static bool IsSafe(params string?[] values) =>
        values.All(value => string.IsNullOrWhiteSpace(value) || !UnsafeContent().IsMatch(value));

    [GeneratedRegex(
        @"\b(nude|nudes|nudity|naked|topless|bottomless|shirtless|erotic|erotica|porn|pornographic|nsfw|sex|sexual|sexy|breast|breasts|nipple|nipples|cleavage|genital|genitals|genitalia|buttock|buttocks|thong|lingerie|underwear|undergarment|panties|bra|bikini|swimsuit|swimwear|swimming costume|bathing suit|burlesque|striptease|stripper|fetish|bdsm|provocative|seductive|akt)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnsafeContent();
}
