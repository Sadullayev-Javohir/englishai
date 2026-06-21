using Domain.Common;

namespace Domain.Competition;

/// <summary>A learner who joined a competition. Tracks live score and per-slide answers.</summary>
public sealed class Participant
{
    private readonly List<SlideAnswer> _answers = new();

    // Parameterless ctor for EF Core materialization.
    private Participant()
    {
        DisplayName = null!;
    }

    private Participant(Guid learnerId, string displayName, bool isHost)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        DisplayName = displayName;
        IsHost = isHost;
        Status = ParticipantStatus.Joined;
        Score = 0;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsHost { get; private set; }
    public ParticipantStatus Status { get; private set; }
    public int Score { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public IReadOnlyList<SlideAnswer> Answers => _answers;

    public static Participant Create(Guid learnerId, string displayName, bool isHost = false)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Participant learner id must not be empty.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Participant display name must not be empty.");
        return new Participant(learnerId, displayName.Trim(), isHost);
    }

    /// <summary>Records an answer, awards speed-scaled points via the settings policy, advances status.</summary>
    public void RecordAnswer(
        CompetitionSlide slide, int selectedOptionIndex, CompetitionSettings settings, double timeRatioRemaining)
    {
        if (Status == ParticipantStatus.Finished)
            throw new DomainException("Participant has already finished.");

        var correct = slide.IsCorrect(selectedOptionIndex);
        var points = correct ? settings.PointsForCorrect(timeRatioRemaining) : 0;
        Score += points;
        _answers.Add(SlideAnswer.Create(slide.Id, selectedOptionIndex, correct, points, DateTimeOffset.UtcNow));

        if (Status == ParticipantStatus.Joined)
            Status = ParticipantStatus.Playing;
    }

    public void MarkFinished() => Status = ParticipantStatus.Finished;

    /// <summary>True when the participant has answered every slide in the competition.</summary>
    public bool HasAnsweredAll(int totalSlides) => _answers.Count >= totalSlides && totalSlides > 0;
}

/// <summary>One recorded answer from a participant for a given slide.</summary>
public sealed class SlideAnswer
{
    private SlideAnswer() { }

    private SlideAnswer(Guid slideId, int selectedOptionIndex, bool isCorrect, int pointsAwarded, DateTimeOffset answeredAt)
    {
        Id = Guid.NewGuid();
        SlideId = slideId;
        SelectedOptionIndex = selectedOptionIndex;
        IsCorrect = isCorrect;
        PointsAwarded = pointsAwarded;
        AnsweredAt = answeredAt;
    }

    public Guid Id { get; private set; }
    public Guid SlideId { get; private set; }
    public int SelectedOptionIndex { get; private set; }
    public bool IsCorrect { get; private set; }
    public int PointsAwarded { get; private set; }
    public DateTimeOffset AnsweredAt { get; private set; }

    public static SlideAnswer Create(
        Guid slideId, int selectedOptionIndex, bool isCorrect, int pointsAwarded, DateTimeOffset answeredAt)
        => new(slideId, selectedOptionIndex, isCorrect, pointsAwarded, answeredAt);
}
