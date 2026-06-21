namespace Domain.Video;

/// <summary>
/// One notable word from a video's transcript paired with a short Uzbek meaning, shown when a
/// learner taps/hovers an unknown word in the player (PROJECT-SPEC B.3, Bosqich 3). The English
/// <see cref="Word"/> is real (from the captions); the <see cref="UzbekMeaning"/> is translated
/// content - the unavoidable-dynamic-text exception of docs/development-guide.md rule 11. Persisted as part of
/// the lesson's glossary JSON column.
/// </summary>
public sealed record VideoGlossaryEntry(string Word, string UzbekMeaning);
