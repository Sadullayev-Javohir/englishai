using FluentAssertions;
using Infrastructure.Video;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the baked, shipped caption tracks (SeedTranscripts/*.json3) parse into real, usable
/// transcripts - this is what lets prod serve the curated catalog's interactive transcript without
/// reaching YouTube from its blocked datacenter IP. A regression here (a truncated/garbage resource)
/// would silently leave a lesson transcript-less, so it is asserted in CI.
/// </summary>
public class SeedTranscriptStoreTests
{
    [Fact]
    public void At_least_the_curated_catalog_has_baked_transcripts()
    {
        // The catalog was baked with ~14 tracks; guard against the resources being dropped from the build.
        SeedTranscriptStore.AvailableVideoIds.Should().HaveCountGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Every_baked_transcript_parses_to_timed_lines_with_word_offsets()
    {
        foreach (var id in SeedTranscriptStore.AvailableVideoIds)
        {
            var segments = SeedTranscriptStore.Load(id);

            segments.Should().NotBeEmpty($"baked transcript {id}.json3 must parse to lines");
            segments.Should().OnlyContain(s => s.EndSeconds > s.StartSeconds, $"{id} lines must be ordered in time");
            segments.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.EnglishText), $"{id} lines must carry text");
            // The karaoke highlight needs per-word timing on at least some lines.
            segments.Should().Contain(s => s.Words.Count > 0, $"{id} must carry per-word timing for the karaoke highlight");
        }
    }

    [Fact]
    public void Load_returns_empty_for_an_unknown_video()
    {
        SeedTranscriptStore.Load("nonexistent00").Should().BeEmpty();
    }

    [Fact]
    public void Catalog_lessons_are_seeded_with_their_baked_transcript_available()
    {
        var lessons = VideoCatalogSeed.Lessons();

        // Every catalog video that has a baked track must seed WITH a ready (Available) transcript, so
        // the player loads it without ever fetching from YouTube - the whole point of the prod fix.
        var withBaked = lessons
            .Where(l => SeedTranscriptStore.AvailableVideoIds.Contains(l.YouTubeVideoId))
            .ToList();

        withBaked.Should().NotBeEmpty();
        withBaked.Should().OnlyContain(l => l.Transcript.Count > 0);
        withBaked.Should().OnlyContain(l => l.TranscriptStatus == Domain.Video.TranscriptStatus.Available);

        // The retired dead video must not be in the catalog anymore.
        lessons.Should().NotContain(l => l.YouTubeVideoId == "8irSFvoyLHQ");
    }
}
