using System.Text;
using Application.Listening.Models;
using Application.Listening.Ports;
using Domain.Assessment;

namespace Infrastructure.Listening;

/// <summary>
/// Deterministic local stand-in for <see cref="IListeningContentGenerator"/>, used when no LLM key
/// is configured so the Listening module runs and is testable offline (same gating pattern as
/// <c>LocalReadingContentGenerator</c>). It weaves a short, CEFR-leveled English transcript around
/// the topic title (the spoken script that Azure TTS synthesizes to audio) and three comprehension
/// questions answerable from the clip - each with an English explanation (the immersion teaching
/// note). Real topic-specific content comes from <see cref="LlmListeningContentGenerator"/> in
/// production.
/// </summary>
public sealed class LocalListeningContentGenerator : IListeningContentGenerator
{
    public Task<GeneratedListeningContent> GenerateAsync(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default)
    {
        var lower = title.ToLowerInvariant();

        // A short spoken monologue about the topic. Kept simple and self-contained so the three
        // questions below are answerable purely from listening.
        var transcript = new StringBuilder()
            .Append("Hello, and welcome. Today I want to talk about ").Append(lower).Append(". ")
            .Append("This is something many people think about, and it can be useful in everyday life. ")
            .Append("When we talk about ").Append(lower)
            .Append(", we usually notice three important things: the people involved, what happens, and why it matters. ")
            .Append("First, listen for who is speaking. Second, listen for what they do. ")
            .Append("And third, try to understand the main reason behind it. ")
            .Append("If you can answer these questions, you have understood the most important part. ")
            .Append("Thank you for listening, and keep practising every day.")
            .ToString();

        var questions = new List<GeneratedListeningQuestion>
        {
            new(
                $"What is the speaker mainly talking about?",
                new List<string> { title, "A football match", "A cooking recipe", "A space mission" },
                0,
                $"The speaker introduces and discusses \"{title}\" from the start, so that is the main topic."),
            new(
                "According to the speaker, how many important things do we usually notice?",
                new List<string> { "Two", "Three", "Four", "Five" },
                1,
                "The speaker clearly lists three things: the people, what happens, and why it matters."),
            new(
                "What does the speaker advise at the end?",
                new List<string>
                {
                    "To keep practising every day",
                    "To stop listening",
                    "To travel abroad",
                    "To buy a new phone",
                },
                0,
                "The speaker closes by saying \"keep practising every day\"."),
        };

        return Task.FromResult(new GeneratedListeningContent(transcript, questions));
    }
}
