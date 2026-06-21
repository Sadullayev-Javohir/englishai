using System.Security.Cryptography;
using System.Text;
using Application.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Infrastructure.Images;

/// <summary>
/// Last-resort vocabulary artwork. It renders the exact English word (and its Uzbek meaning) into a
/// deterministic, locally-created WebP card. No external content is involved, so the result is safe
/// by construction and a word can always be replaced even when photo providers have no suitable hit.
/// </summary>
public static class GeneratedVocabularyImageFactory
{
    private const int Width = 640;
    private const int Height = 480;

    public static DownloadedImage Create(string word, string translation, long version)
    {
        var normalizedWord = string.IsNullOrWhiteSpace(word) ? "WORD" : word.Trim().ToUpperInvariant();
        var normalizedTranslation = translation.Trim().ToUpperInvariant();
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{normalizedWord}:{version}"));
        var background = new Rgb24((byte)(35 + digest[0] % 90), (byte)(70 + digest[1] % 100), (byte)(95 + digest[2] % 110));
        var accent = new Rgb24((byte)(155 + digest[3] % 90), (byte)(155 + digest[4] % 90), (byte)(155 + digest[5] % 90));

        using var image = new Image<Rgb24>(Width, Height, background);
        FillRect(image, 32, 32, Width - 64, Height - 64, new Rgb24(248, 250, 249));
        FillRect(image, 32, 32, 18, Height - 64, accent);
        DrawCentered(image, normalizedWord, 95, 290, new Rgb24(23, 37, 30), maxScale: 12);
        if (normalizedTranslation.Length > 0)
            DrawCentered(image, normalizedTranslation, 330, 405, new Rgb24(77, 96, 86), maxScale: 5);

        using var stream = new MemoryStream();
        image.Save(stream, new WebpEncoder { Quality = 88 });
        return new DownloadedImage(
            stream.ToArray(),
            WebpConverter.WebpContentType,
            "EnglishAI Generated",
            "EnglishAI generated vocabulary illustration",
            $"generated:vocabulary:{version}",
            Width,
            Height,
            ImageSafetyStatus.Safe,
            "local-safe-v1",
            DateTimeOffset.UtcNow,
            "Locally generated text artwork; no external or adult content.");
    }

    private static void DrawCentered(Image<Rgb24> image, string text, int top, int bottom, Rgb24 color, int maxScale)
    {
        var lines = Wrap(text, 18);
        var scale = Math.Max(2, Math.Min(maxScale, (Width - 120) / Math.Max(1, lines.Max(LineWidth))));
        var lineHeight = 8 * scale;
        var totalHeight = lines.Count * lineHeight;
        var y = top + Math.Max(0, (bottom - top - totalHeight) / 2);
        foreach (var line in lines)
        {
            var x = (Width - LineWidth(line) * scale) / 2;
            foreach (var character in line)
            {
                DrawGlyph(image, character, x, y, scale, color);
                x += 6 * scale;
            }
            y += lineHeight;
        }
    }

    private static IReadOnlyList<string> Wrap(string value, int maxCharacters)
    {
        var lines = new List<string>();
        foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (lines.Count == 0 || lines[^1].Length + token.Length + 1 > maxCharacters)
                lines.Add(token[..Math.Min(token.Length, maxCharacters)]);
            else
                lines[^1] += " " + token;
        }
        return lines.Count == 0 ? ["WORD"] : lines.Take(3).ToArray();
    }

    private static int LineWidth(string line) => Math.Max(1, line.Length * 6 - 1);

    private static void DrawGlyph(Image<Rgb24> image, char character, int x, int y, int scale, Rgb24 color)
    {
        if (!Glyphs.TryGetValue(character, out var rows)) rows = Glyphs['?'];
        for (var row = 0; row < rows.Length; row++)
            for (var column = 0; column < 5; column++)
                if ((rows[row] & (1 << (4 - column))) != 0)
                    FillRect(image, x + column * scale, y + row * scale, scale, scale, color);
    }

    private static void FillRect(Image<Rgb24> image, int x, int y, int width, int height, Rgb24 color)
    {
        var right = Math.Min(image.Width, x + width);
        var bottom = Math.Min(image.Height, y + height);
        for (var py = Math.Max(0, y); py < bottom; py++)
            for (var px = Math.Max(0, x); px < right; px++)
                image[px, py] = color;
    }

    private static readonly IReadOnlyDictionary<char, byte[]> Glyphs = new Dictionary<char, byte[]>
    {
        [' '] = [0,0,0,0,0,0,0], ['?'] = [14,17,1,2,4,0,4], ['-'] = [0,0,0,31,0,0,0],
        ['A']=[14,17,17,31,17,17,17], ['B']=[30,17,17,30,17,17,30], ['C']=[14,17,16,16,16,17,14],
        ['D']=[30,17,17,17,17,17,30], ['E']=[31,16,16,30,16,16,31], ['F']=[31,16,16,30,16,16,16],
        ['G']=[14,17,16,23,17,17,15], ['H']=[17,17,17,31,17,17,17], ['I']=[14,4,4,4,4,4,14],
        ['J']=[7,2,2,2,18,18,12], ['K']=[17,18,20,24,20,18,17], ['L']=[16,16,16,16,16,16,31],
        ['M']=[17,27,21,21,17,17,17], ['N']=[17,25,21,19,17,17,17], ['O']=[14,17,17,17,17,17,14],
        ['P']=[30,17,17,30,16,16,16], ['Q']=[14,17,17,17,21,18,13], ['R']=[30,17,17,30,20,18,17],
        ['S']=[15,16,16,14,1,1,30], ['T']=[31,4,4,4,4,4,4], ['U']=[17,17,17,17,17,17,14],
        ['V']=[17,17,17,17,17,10,4], ['W']=[17,17,17,21,21,21,10], ['X']=[17,17,10,4,10,17,17],
        ['Y']=[17,17,10,4,4,4,4], ['Z']=[31,1,2,4,8,16,31],
        ['0']=[14,17,19,21,25,17,14], ['1']=[4,12,4,4,4,4,14], ['2']=[14,17,1,2,4,8,31],
        ['3']=[30,1,1,14,1,1,30], ['4']=[2,6,10,18,31,2,2], ['5']=[31,16,16,30,1,1,30],
        ['6']=[14,16,16,30,17,17,14], ['7']=[31,1,2,4,8,8,8], ['8']=[14,17,17,14,17,17,14],
        ['9']=[14,17,17,15,1,1,14],
    };
}
