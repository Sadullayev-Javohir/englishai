namespace Application.Writing.Models;

/// <summary>
/// The LLM-generated writing task for a learning-spine topic: a CEFR-leveled English prompt
/// (a concrete writing scenario about the topic) and a few short English "what to include" hints
/// (immersion teaching support - richer at A1–A2, sparser by B1+). Produced by an
/// <see cref="Ports.IWritingPromptGenerator"/> and cached on the task (rules 8, 10). The word
/// range is fixed from the level, so it is not part of the generated content.
/// </summary>
public sealed record GeneratedWritingPrompt(string Prompt, IReadOnlyList<string> Guidance)
{
    /// <summary>An empty result, signalling generation was unavailable (task stays pending).</summary>
    public static GeneratedWritingPrompt Empty { get; } =
        new(string.Empty, Array.Empty<string>());

    /// <summary>True when there is a usable prompt.</summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Prompt);
}
