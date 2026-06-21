using Domain.Assessment;

namespace Application.Gamification.Dtos;

/// <summary>A single leaderboard row, ready for display.</summary>
public sealed record LeaderboardEntryDto(
    int Rank,
    Guid LearnerId,
    string DisplayName,
    string? PictureUrl,
    long Score,
    bool IsPremium,
    bool IsCurrentUser);

/// <summary>
/// One CEFR level's leaderboard: the top entries, plus the viewer's own row when they rank
/// outside of it (e.g. "100-o'rindasiz" pinned below the top 50).
/// </summary>
public sealed record LeaderboardDto(
    CefrLevel Level,
    IReadOnlyList<LeaderboardEntryDto> Top,
    LeaderboardEntryDto? CurrentUserEntry);
