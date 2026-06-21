using Application.Video.Dtos;
using Application.Video.GetRealtimeTranscript;
using Application.Video.Models;
using Application.Video.Ports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Video;

/// <summary>
/// Verifies the real-time transcript query maps the orchestrator's outcome to a client status and never
/// persists anything (it has no repository dependency at all): Fetched → Available with lines,
/// NoCaptions → Unavailable, ProviderUnavailable → Pending (docs/development-guide.md real-time rule + rule 8).
/// </summary>
public class GetRealtimeTranscriptQueryHandlerTests
{
    private readonly IVideoTranscriptProvider _transcripts = Substitute.For<IVideoTranscriptProvider>();

    private GetRealtimeTranscriptQueryHandler Build() => new(_transcripts);

    [Fact]
    public async Task Fetched_lines_become_available_with_segments()
    {
        _transcripts.FetchAsync("vid12345678", Arg.Any<CancellationToken>())
            .Returns(TranscriptFetchResult.Fetched(new[]
            {
                new TranscriptLine(0, 2, "Hello world."),
                new TranscriptLine(2, 4, "Second line."),
            }));

        var result = await Build().Handle(new GetRealtimeTranscriptQuery("vid12345678"), CancellationToken.None);

        result.Status.Should().Be(RealtimeTranscriptStatus.Available);
        result.Lines.Should().HaveCount(2);
        result.Lines[0].EnglishText.Should().Be("Hello world.");
    }

    [Fact]
    public async Task No_captions_becomes_unavailable_with_no_lines()
    {
        _transcripts.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TranscriptFetchResult.NoCaptions);

        var result = await Build().Handle(new GetRealtimeTranscriptQuery("vid12345678"), CancellationToken.None);

        result.Status.Should().Be(RealtimeTranscriptStatus.Unavailable);
        result.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task Transient_failure_becomes_pending_for_retry()
    {
        _transcripts.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TranscriptFetchResult.ProviderUnavailable);

        var result = await Build().Handle(new GetRealtimeTranscriptQuery("vid12345678"), CancellationToken.None);

        result.Status.Should().Be(RealtimeTranscriptStatus.Pending);
        result.Lines.Should().BeEmpty();
    }
}
