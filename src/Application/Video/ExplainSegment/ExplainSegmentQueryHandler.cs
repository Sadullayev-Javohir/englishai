using Application.Common;
using Application.Video.Dtos;
using Application.Video.Ports;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace Application.Video.ExplainSegment;

/// <summary>
/// Resolves one explain-chat answer. Repeated identical questions are cached, while transcript and
/// history context are bounded server-side regardless of what the client sends. A failed explanation
/// returns a null reply (honest "unavailable") rather than fabricated text (rules 8, 10, 11).
/// </summary>
public sealed class ExplainSegmentQueryHandler : IRequestHandler<ExplainSegmentQuery, ChatReplyDto>
{
    /// <summary>Trailing turns kept for follow-up context - bounds the prompt (rule 10).</summary>
    private const int MaxHistoryTurns = 4;
    private const int MaxTranscriptCharacters = 6000;

    private readonly IChatExplainer _explainer;
    private readonly IVideoRepository _videos;
    private readonly IVideoExplainCache _cache;
    private readonly IVideoExplainCoordinator? _coordinator;
    private readonly ICurrentUserAccessor? _currentUser;

    public ExplainSegmentQueryHandler(
        IChatExplainer explainer,
        IVideoRepository videos,
        IVideoExplainCache cache,
        IVideoExplainCoordinator? coordinator = null,
        ICurrentUserAccessor? currentUser = null)
    {
        _explainer = explainer;
        _videos = videos;
        _cache = cache;
        _coordinator = coordinator;
        _currentUser = currentUser;
    }

    public async Task<ChatReplyDto> Handle(ExplainSegmentQuery request, CancellationToken cancellationToken)
    {
        var lesson = await _videos.GetByIdAsync(request.VideoLessonId, cancellationToken)
                     ?? throw new NotFoundException("Video lesson", request.VideoLessonId);
        var transcript = lesson.Transcript
            .OrderBy(segment => segment.StartSeconds)
            .Select(segment => segment.EnglishText.Trim())
            .Where(text => text.Length > 0)
            .ToList();
        var fullTranscript = BuildRelevantTranscript(transcript, request.FocusText.Trim());
        var history = request.History
            .TakeLast(MaxHistoryTurns)
            .Select(t => new ChatTurn(t.Role, t.Text))
            .ToList();
        var focusText = request.FocusText.Trim();
        var userMessage = request.UserMessage.Trim();
        var cacheKey = CacheKey(request.VideoLessonId, focusText, userMessage);

        var cached = await _cache.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
            return new ChatReplyDto(cached);

        var reply = _coordinator is null
            ? await _explainer.ExplainAsync(lesson.Title, fullTranscript, focusText, userMessage, history, cancellationToken)
            : await _coordinator.ExecuteAsync(
                new VideoExplainWork(cacheKey, lesson.Title, fullTranscript, focusText, userMessage, history),
                _currentUser?.LearnerId?.ToString() ?? "anonymous",
                cancellationToken);

        if (!string.IsNullOrWhiteSpace(reply))
            await _cache.SetAsync(cacheKey, reply.Trim(), cancellationToken);

        return new ChatReplyDto(string.IsNullOrWhiteSpace(reply) ? null : reply.Trim());
    }

    private static string BuildRelevantTranscript(IReadOnlyList<string> lines, string focusText)
    {
        if (lines.Count == 0)
            return string.Empty;

        var selected = new List<string>();
        var focusIndex = lines
            .Select((line, index) => (line, index))
            .FirstOrDefault(item => string.Equals(item.line, focusText, StringComparison.OrdinalIgnoreCase))
            .index;

        if (!string.IsNullOrWhiteSpace(focusText) && lines.Any(line => string.Equals(line, focusText, StringComparison.OrdinalIgnoreCase)))
        {
            var start = Math.Max(0, focusIndex - 4);
            var end = Math.Min(lines.Count - 1, focusIndex + 4);
            selected.AddRange(lines.Skip(start).Take(end - start + 1));
        }

        var remaining = MaxTranscriptCharacters - selected.Sum(line => line.Length + 1);
        foreach (var line in lines)
        {
            if (selected.Contains(line, StringComparer.OrdinalIgnoreCase))
                continue;
            if (line.Length + 1 > remaining)
                break;
            selected.Add(line);
            remaining -= line.Length + 1;
        }

        return string.Join('\n', selected);
    }

    private static string CacheKey(
        Guid lessonId,
        string focusText,
        string userMessage)
    {
        var normalizedQuestion = string.Join(' ', userMessage.ToLowerInvariant().Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var raw = new StringBuilder()
            .Append("v2|").Append(lessonId).Append('|')
            .Append(focusText.ToLowerInvariant()).Append('|')
            .Append(normalizedQuestion);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw.ToString())));
    }
}
