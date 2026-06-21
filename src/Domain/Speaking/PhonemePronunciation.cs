namespace Domain.Speaking;

/// <summary>
/// Accuracy of a single phoneme within a spoken word. <see cref="Phoneme"/> is the
/// IPA symbol; <see cref="AccuracyScore"/> is 0-100.
/// </summary>
public sealed record PhonemePronunciation(string Phoneme, double AccuracyScore);
