using Domain.Competition;

namespace Application.Competition.Dtos;

/// <summary>Host-tunable competition rules (see <see cref="CompetitionSettings"/>).</summary>
public sealed record CompetitionSettingsDto(
    int SlideDurationSeconds,
    bool AutoAdvance,
    bool ShuffleSlides,
    bool AllowLateJoin,
    int QuestionsPerTopic,
    bool ShowLiveLeaderboard,
    int BasePointsPerCorrect)
{
    public static CompetitionSettingsDto FromDomain(CompetitionSettings s) => new(
        s.SlideDurationSeconds, s.AutoAdvance, s.ShuffleSlides, s.AllowLateJoin,
        s.QuestionsPerTopic, s.ShowLiveLeaderboard, s.BasePointsPerCorrect);
}

/// <summary>A single question in the live game (sent to clients; correct index is NEVER exposed until graded).</summary>
public sealed record SlideDto(
    Guid Id,
    int Order,
    SlideSourceType SourceType,
    Guid SourceTopicId,
    string QuestionText,
    IReadOnlyList<string> Options,
    int Points);

/// <summary>Participant summary broadcast to the lobby / live board.</summary>
public sealed record ParticipantDto(
    Guid Id,
    Guid LearnerId,
    string DisplayName,
    bool IsHost,
    ParticipantStatus Status,
    int Score);

/// <summary>A topic the host may pick (vocabulary or grammar) for the competition.</summary>
public sealed record SelectableTopicDto(
    Guid Id,
    string Title,
    string TitleUz,
    string Source, // "vocabulary" | "grammar"
    string Category);

/// <summary>Full competition view returned to participants (no access code, no correct answers).</summary>
public sealed record CompetitionDto(
    Guid Id,
    string Title,
    CompetitionStatus Status,
    CompetitionSettingsDto Settings,
    int CurrentSlideIndex,
    int TotalSlides,
    IReadOnlyList<ParticipantDto> Participants,
    // Only the host sees this (returned from create/join-as-host).
    string? AccessCode = null);

/// <summary>Live ranking row broadcast during/after the game.</summary>
public sealed record RankingRowDto(
    Guid ParticipantId,
    string DisplayName,
    bool IsHost,
    int Score,
    int AnsweredCount,
    int Rank);

/// <summary>Final results with podium + reward summary.</summary>
public sealed record CompetitionResultDto(
    Guid CompetitionId,
    string Title,
    IReadOnlyList<RankingRowDto> Ranking,
    Guid WinnerParticipantId,
    string WinnerDisplayName,
    int WinnerScore,
    int RewardPoints);
