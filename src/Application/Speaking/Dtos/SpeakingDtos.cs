using Domain.Speaking;

namespace Application.Speaking.Dtos;

/// <summary>
/// A viseme keyframe expressed for the client (viseme id + audio offset in milliseconds), used
/// for timing the "speaking" indicator. The actual mouth animation is the whole-utterance SVG
/// carried separately on each result (<c>VisemeAnimation</c>) - Azure <c>redlips_front</c>
/// produces one SVG for the entire utterance, not one per frame.
/// </summary>
public sealed record VisemeFrameDto(int VisemeId, double OffsetMs)
{
    public static VisemeFrameDto FromDomain(VisemeFrame frame) =>
        new(frame.VisemeId, frame.AudioOffset.TotalMilliseconds);

    public static IReadOnlyList<VisemeFrameDto> FromDomain(VisemeSequence sequence) =>
        sequence.Frames.Select(FromDomain).ToList();
}

public sealed record SpeechWordTimingDto(
    string Text,
    int TextOffset,
    int WordLength,
    double AudioOffsetMs,
    double DurationMs)
{
    public static SpeechWordTimingDto FromDomain(Models.SpeechWordTiming timing) =>
        new(
            timing.Text,
            timing.TextOffset,
            timing.WordLength,
            timing.AudioOffset.TotalMilliseconds,
            timing.Duration.TotalMilliseconds);

    public static IReadOnlyList<SpeechWordTimingDto> FromDomain(
        IReadOnlyList<Models.SpeechWordTiming> timings) =>
        timings.Select(FromDomain).ToList();
}

public sealed record PhonemePronunciationDto(string Phoneme, double AccuracyScore)
{
    public static PhonemePronunciationDto FromDomain(PhonemePronunciation phoneme) =>
        new(phoneme.Phoneme, Math.Round(phoneme.AccuracyScore, 1));
}

public sealed record WordPronunciationDto(
    string Word,
    double AccuracyScore,
    PronunciationErrorType ErrorType,
    bool NeedsPractice,
    IReadOnlyList<PhonemePronunciationDto> Phonemes,
    string? SpokenForm)
{
    public static WordPronunciationDto FromDomain(WordPronunciation word) =>
        new(
            word.Word,
            Math.Round(word.AccuracyScore, 1),
            word.ErrorType,
            word.NeedsPractice,
            word.Phonemes.Select(PhonemePronunciationDto.FromDomain).ToList(),
            word.SpokenForm);
}

public sealed record PronunciationResultDto(
    double OverallScore,
    double AccuracyScore,
    double FluencyScore,
    double CompletenessScore,
    PronunciationBand Band,
    bool IsAuthentic,
    IReadOnlyList<WordPronunciationDto> Words)
{
    public static PronunciationResultDto FromDomain(PronunciationResult result) =>
        new(
            Math.Round(result.OverallScore, 1),
            Math.Round(result.AccuracyScore, 1),
            Math.Round(result.FluencyScore, 1),
            Math.Round(result.CompletenessScore, 1),
            result.Band,
            result.IsAuthentic,
            result.Words.Select(WordPronunciationDto.FromDomain).ToList());
}

/// <summary>Phoneme + its SVG mouth-shape, for the pronunciation detail screen.</summary>
public sealed record PhonemeVisualDto(string Phoneme, string SvgId, bool IsHardForUzbek);

/// <summary>
/// One idea card for the "I don't know what to say" helper in the live conversation. English content
/// only (<see cref="Prompt"/> = a concrete talking point, <see cref="Starter"/> = a sentence opener,
/// both in the target language); the surrounding Uzbek labels are frontend templates (rule 11).
/// <see cref="Emoji"/> is the card's fallback picture when the client has no richer topic image.
/// </summary>
public sealed record IdeaCardDto(string Prompt, string Starter, string Emoji)
{
    public static IdeaCardDto FromDomain(IdeaCard card) => new(card.Prompt, card.Starter, card.Emoji);
}

/// <summary>The set of idea cards returned for a conversation when the learner asks for help.</summary>
public sealed record IdeaCardsResult(IReadOnlyList<IdeaCardDto> Cards);

/// <summary>
/// The learner's progress toward learning the linked vocabulary topic by speaking about it
/// (PROJECT-SPEC module 4 ↔ Faza 1, the 5-minute rule). Present only when the conversation is
/// linked to a topic; null for a free conversation.
/// </summary>
/// <param name="SpokenSeconds">Cumulative engaged-speaking time for the topic so far.</param>
/// <param name="GoalSeconds">Speaking time needed to learn the topic (300 = 5 minutes).</param>
/// <param name="Learned">Whether the topic is now learned (goal reached).</param>
/// <param name="JustLearned">True only on the utterance that crossed the goal - for a one-off celebration.</param>
public sealed record TopicSpeakingProgressDto(int SpokenSeconds, int GoalSeconds, bool Learned, bool JustLearned);

public sealed record WordPronunciationDetailDto(
    string Word,
    string? SpokenForm,
    string Ipa,
    IReadOnlyList<PhonemeVisualDto> Phonemes,
    string? TipUz,
    // The focus word / keyword callout. Defaults to the word itself but kept nullable so
    // a future curated variant (e.g. a stress-syllable highlight) can differ from the word.
    string? KeyWord,
    // A verified Uzbek example sentence demonstrating the word in context, shown in the 3D
    // card. Sourced only from the curated content store (docs/development-guide.md rule 11) - never free
    // LLM-generated Uzbek. Null when no curated example exists (card renders conditionally).
    string? ExampleSentenceUz,
    IReadOnlyList<VisemeFrameDto> Visemes,
    string? VisemeAnimation,
    // The Azure reference voice for the whole word as a base64 WAV. The detail screen plays
    // this so audio works regardless of the browser's built-in speech-synthesis support
    // (Linux/Chromium clients often have no installed voices). Null only when synthesis
    // produced no audio (offline fallback) - the screen then falls back to the browser voice.
    string? AudioBase64);
