namespace Domain.Speaking;

/// <summary>
/// One mouth-shape keyframe: the viseme id and the audio offset at which it becomes
/// active, produced by the TTS engine alongside the synthesized audio (PROJECT-SPEC B.2).
/// The mouth animation itself is a single whole-utterance SVG on the owning sequence
/// (<see cref="VisemeSequence.Animation"/>) - Azure <c>redlips_front</c> emits one SVG for
/// the entire utterance, not one per frame - so frames are used for timing, not rendering.
/// </summary>
public sealed record VisemeFrame(int VisemeId, TimeSpan AudioOffset);
