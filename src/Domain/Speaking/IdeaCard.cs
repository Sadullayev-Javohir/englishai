namespace Domain.Speaking;

/// <summary>
/// One "idea card" the learner can pull up when they run out of things to say - the core cure for
/// the blank-page freeze in speaking practice. It carries only English content on purpose: the
/// <see cref="Prompt"/> is a concrete, answerable talking-point question and the <see cref="Starter"/>
/// is a sentence opener the learner can finish in their own words - both in the target language, which
/// is exactly what we want the AI to produce (docs/development-guide.md rule 11 forbids the AI from writing free
/// Uzbek prose, not English learning content). The learner-facing Uzbek framing labels around the
/// cards live in the frontend content store. <see cref="Emoji"/> is a single pictograph used as the
/// card's picture when no richer topic image is available on the client.
/// </summary>
public sealed record IdeaCard(string Prompt, string Starter, string Emoji);
