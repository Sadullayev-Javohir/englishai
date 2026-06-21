using System.Security.Cryptography;
using System.Text;
using Domain.Common;
using Domain.Grammar;
using Domain.Vocabulary;

namespace Domain.Competition;

/// <summary>
/// Aggregate root for a live, multi-player "Musobaqa" (competition). The host selects vocabulary
/// and grammar topics; the competition auto-builds a deterministic slide sequence from those
/// topics' existing quiz/exercise material. Participants join with a host-shared access code
/// (stored only as a salted hash, docs/development-guide.md rule 13). The host starts the game; slides advance
/// (auto or by host) and answers are scored live with a speed bonus.
/// </summary>
public sealed class Competition
{
    private const int AccessCodeLength = 6;
    private static readonly char[] AccessCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    private readonly List<CompetitionSlide> _slides = new();
    private readonly List<Participant> _participants = new();
    private readonly List<Guid> _topicIds = new();

    private Competition()
    {
        Title = null!;
        AccessCodeHash = null!;
        AccessCodeSalt = null!;
        Settings = null!;
    }

    private Competition(
        Guid hostLearnerId, string hostDisplayName, string title,
        CompetitionSettings settings, string accessCodeHash, string accessCodeSalt,
        IEnumerable<Guid> topicIds)
    {
        Id = Guid.NewGuid();
        HostLearnerId = hostLearnerId;
        Title = title.Trim();
        Settings = settings;
        AccessCodeHash = accessCodeHash;
        AccessCodeSalt = accessCodeSalt;
        Status = CompetitionStatus.Lobby;
        CurrentSlideIndex = -1;
        CreatedAt = DateTimeOffset.UtcNow;
        _topicIds.AddRange(topicIds);
        _participants.Add(Participant.Create(hostLearnerId, hostDisplayName, isHost: true));
    }

    public Guid Id { get; private set; }
    public Guid HostLearnerId { get; private set; }
    public string Title { get; private set; }
    public CompetitionStatus Status { get; private set; }
    public CompetitionSettings Settings { get; private set; }
    public IReadOnlyList<CompetitionSlide> Slides => _slides;
    public IReadOnlyList<Participant> Participants => _participants;
    public IReadOnlyList<Guid> TopicIds => _topicIds;
    public int CurrentSlideIndex { get; private set; }
    public string AccessCodeHash { get; private set; }
    public string AccessCodeSalt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }

    /// <summary>Generates a human-friendly 6-char access code and returns (code, hash, salt).</summary>
    public static (string code, string hash, string salt) GenerateAccessCode()
    {
        var code = new StringBuilder(AccessCodeLength);
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[1];
        for (var i = 0; i < AccessCodeLength; i++)
        {
            rng.GetBytes(bytes);
            code.Append(AccessCodeAlphabet[bytes[0] % AccessCodeAlphabet.Length]);
        }
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        return (code.ToString(), HashCode(code.ToString(), salt), salt);
    }

    private static string HashCode(string code, string salt)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{salt}:{code.ToUpperInvariant()}"));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyCode(string code, string salt, string hash)
        => string.Equals(HashCode(code, salt), hash, StringComparison.Ordinal);

    public static Competition Create(
        Guid hostLearnerId, string hostDisplayName, string title,
        CompetitionSettings settings, IEnumerable<Guid> topicIds)
    {
        if (hostLearnerId == Guid.Empty)
            throw new DomainException("Host learner id must not be empty.");
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Competition title must not be empty.");
        var ids = topicIds?.ToList() ?? throw new DomainException("Topic ids must not be null.");
        if (ids.Count == 0)
            throw new DomainException("A competition needs at least one topic.");

        var (code, hash, salt) = GenerateAccessCode();
        var competition = new Competition(
            hostLearnerId, hostDisplayName, title, settings, hash, salt, ids);
        competition._accessCode = code; // surfaced once to the host at creation
        return competition;
    }

    // Transient: only populated right after Create so the handler can return the raw code to the host.
    private string? _accessCode;
    public string? AccessCode => _accessCode;

    /// <summary>
    /// Builds the slide sequence from the supplied topic material. Vocabulary topics contribute
    /// <see cref="CompetitionSettings.QuestionsPerTopic"/> cloze questions (via
    /// <see cref="VocabularyTopic.BuildQuiz"/>); grammar topics contribute their exercises. The
    /// order is deterministic so grading is reproducible.
    /// </summary>
    public void BuildSlides(
        IReadOnlyList<VocabularyTopic> vocabularyTopics, IReadOnlyList<GrammarLesson> grammarLessons)
    {
        if (Status != CompetitionStatus.Lobby)
            throw new DomainException("Slides can only be built while in the lobby.");
        if (_slides.Count > 0)
            throw new DomainException("Slides have already been built.");

        const int VocabQuizSizeCap = 8;
        var order = 0;
        var byId = vocabularyTopics.ToDictionary(t => t.Id);
        var grammarById = grammarLessons.ToDictionary(g => g.Id);

        foreach (var topicId in _topicIds)
        {
            if (byId.TryGetValue(topicId, out var vt) && vt.IsFilled)
            {
                var quiz = vt.BuildQuiz(Math.Min(Settings.QuestionsPerTopic, VocabQuizSizeCap));
                foreach (var q in quiz)
                {
                    _slides.Add(CompetitionSlide.Create(
                        order++, SlideSourceType.Vocabulary, topicId, q.Prompt,
                        q.Options, q.CorrectOptionIndex, Settings.BasePointsPerCorrect));
                }
            }
            else if (grammarById.TryGetValue(topicId, out var gl))
            {
                var take = Math.Min(Settings.QuestionsPerTopic, gl.Exercises.Count);
                foreach (var ex in gl.Exercises.Take(take))
                {
                    _slides.Add(CompetitionSlide.Create(
                        order++, SlideSourceType.Grammar, topicId, ex.Prompt,
                        ex.Options, ex.CorrectOptionIndex, Settings.BasePointsPerCorrect));
                }
            }
        }

        if (_slides.Count == 0)
            throw new DomainException("Selected topics produced no questions.");

        if (Settings.ShuffleSlides)
            Shuffle(_slides, Id);
    }

    private static void Shuffle(List<CompetitionSlide> list, Guid competitionId)
    {
        var rng = new Random(GuidHash(competitionId));
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        for (var i = 0; i < list.Count; i++)
            list[i] = WithOrder(list[i], i);
    }

    private static CompetitionSlide WithOrder(CompetitionSlide s, int order)
        => CompetitionSlide.Create(
            order, s.SourceType, s.SourceTopicId, s.QuestionText, s.Options, s.CorrectOptionIndex, s.Points);

    private static int GuidHash(Guid id)
    {
        unchecked
        {
            var h = 17;
            foreach (var b in id.ToByteArray()) h = h * 31 + b;
            return h;
        }
    }

    /// <summary>Host starts the game. Requires slides to have been built.</summary>
    public void Start(Guid hostLearnerId)
    {
        if (hostLearnerId != HostLearnerId)
            throw new DomainException("Only the host can start the competition.");
        if (Status != CompetitionStatus.Lobby)
            throw new DomainException("Competition is not in the lobby.");
        if (_slides.Count == 0)
            throw new DomainException("Cannot start a competition with no slides.");

        Status = CompetitionStatus.Active;
        CurrentSlideIndex = 0;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Joins a participant via the access code. Late-join policy enforced.</summary>
    public Participant Join(Guid learnerId, string displayName, string? code = null)
    {
        if (Status == CompetitionStatus.Finished)
            throw new DomainException("Competition has already finished.");
        if (Status == CompetitionStatus.Active && !Settings.AllowLateJoin)
            throw new DomainException("Late join is not allowed for this competition.");

        // The host joins free; everyone else must present the correct access code.
        var isHost = learnerId == HostLearnerId;
        if (!isHost && !VerifyCode(code ?? string.Empty, AccessCodeSalt, AccessCodeHash))
            throw new DomainException("Noto'g'ri kirish kodi."); // rule 11: vetted Uzbek copy

        var existing = _participants.FirstOrDefault(p => p.LearnerId == learnerId);
        if (existing is not null)
            return existing;

        var participant = Participant.Create(learnerId, displayName, isHost);
        _participants.Add(participant);
        return participant;
    }

    public CompetitionSlide CurrentSlide =>
        Status == CompetitionStatus.Active && CurrentSlideIndex >= 0 && CurrentSlideIndex < _slides.Count
            ? _slides[CurrentSlideIndex]
            : throw new DomainException("No current slide.");

    /// <summary>Records an answer for the given participant on the current slide.</summary>
    public void SubmitAnswer(Guid participantId, int selectedOptionIndex, double timeRatioRemaining)
    {
        if (Status != CompetitionStatus.Active)
            throw new DomainException("Competition is not active.");
        var participant = _participants.FirstOrDefault(p => p.Id == participantId)
            ?? throw new DomainException("Participant not found.");
        var slide = CurrentSlide;
        participant.RecordAnswer(slide, selectedOptionIndex, Settings, timeRatioRemaining);

        if (participant.HasAnsweredAll(_slides.Count))
            participant.MarkFinished();
    }

    /// <summary>Advances to the next slide, or finishes the competition when exhausted.</summary>
    public void Advance()
    {
        if (Status != CompetitionStatus.Active)
            throw new DomainException("Competition is not active.");
        if (CurrentSlideIndex >= _slides.Count - 1)
        {
            Finish();
            return;
        }
        CurrentSlideIndex++;
    }

    public void Finish()
    {
        if (Status == CompetitionStatus.Finished) return;
        Status = CompetitionStatus.Finished;
        FinishedAt = DateTimeOffset.UtcNow;
        foreach (var p in _participants.Where(p => p.Status != ParticipantStatus.Finished))
            p.MarkFinished();
    }

    /// <summary>Final ranking: highest score first, ties broken by who finished more slides.</summary>
    public IReadOnlyList<Participant> Ranking()
        => _participants
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.Answers.Count)
            .ToList();
}
