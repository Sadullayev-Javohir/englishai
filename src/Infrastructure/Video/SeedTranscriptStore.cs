using System.Reflection;
using System.Text.Json;
using Domain.Video;

namespace Infrastructure.Video;

/// <summary>
/// Loads pre-fetched English caption tracks shipped with the app as embedded <c>json3</c> resources
/// (<c>src/Infrastructure/Video/SeedTranscripts/&lt;id&gt;.json3</c>), parsed with the very same
/// <see cref="TranscriptParser.ParseTimedTextJson3"/> a live fetch uses - so the per-word karaoke
/// timing is identical to a freshly fetched transcript.
///
/// WHY this exists: from the prod VPS's datacenter IP YouTube serves a "confirm you're not a bot"
/// challenge, so the live <c>yt-dlp</c> fetch returns nothing and a lesson's interactive transcript
/// never loads. These tracks were captured once from an unblocked (residential) IP and baked into the
/// image, so the curated catalog's transcripts are served straight from the app and prod never has to
/// reach YouTube for them (docs/development-guide.md rule 8 - never leave the learner stuck on the slow/blocked path).
/// </summary>
internal static class SeedTranscriptStore
{
    private const string ResourcePrefix = "Infrastructure.Video.SeedTranscripts.";
    private const string ResourceSuffix = ".json3";
    private static readonly Assembly Asm = typeof(SeedTranscriptStore).Assembly;

    /// <summary>The YouTube ids that ship with a baked transcript resource (discovered once).</summary>
    public static IReadOnlySet<string> AvailableVideoIds { get; } = DiscoverIds();

    /// <summary>
    /// Parses the baked transcript for <paramref name="youTubeVideoId"/> into ready-to-store segments,
    /// or an empty list when no resource exists or it fails to parse (so the caller simply skips it).
    /// </summary>
    public static IReadOnlyList<TranscriptSegment> Load(string youTubeVideoId)
    {
        using var stream = Asm.GetManifestResourceStream(ResourcePrefix + youTubeVideoId + ResourceSuffix);
        if (stream is null)
            return Array.Empty<TranscriptSegment>();

        try
        {
            using var doc = JsonDocument.Parse(stream);
            var lines = TranscriptParser.ParseTimedTextJson3(doc.RootElement);
            return lines
                .Select(l => TranscriptSegment.Create(
                    l.StartSeconds, l.EndSeconds, l.EnglishText, l.UzbekTranslation, l.Words))
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<TranscriptSegment>();
        }
    }

    private static IReadOnlySet<string> DiscoverIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var res in Asm.GetManifestResourceNames())
        {
            if (res.StartsWith(ResourcePrefix, StringComparison.Ordinal) &&
                res.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            {
                ids.Add(res[ResourcePrefix.Length..^ResourceSuffix.Length]);
            }
        }
        return ids;
    }
}
