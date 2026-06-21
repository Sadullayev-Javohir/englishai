using System.Net.Http.Json;
using System.Text.Json;

namespace Integration.Tests;

/// <summary>
/// Drives a placement session to completion over HTTP, dispatching each item by its
/// kind: multiple-choice items go to <c>/answer</c>, the Writing task to
/// <c>/answer/writing</c>, and the Speaking task to <c>/answer/speaking</c>. Shared by the
/// placement and learner-model flow tests so both exercise the full five-skill sequence.
/// </summary>
internal static class PlacementDriver
{
    private const int McqKind = 0;
    private const int WritingKind = 1;
    private const int SpeakingKind = 2;

    // A modest free-text answer and a small recording: enough to pass the non-empty
    // validators and be scored, but weak enough not to inflate the placement result.
    private static readonly byte[] WeakSpeakingAudio = new byte[6400];

    private static string WeakWritingAnswer(JsonElement item)
    {
        var minimum = item.GetProperty("minWords").GetInt32();
        var words = "I usually meet my friends at the weekend and we play football in a small park near my home then we drink tea and talk about school work family and our plans for the next week because this is a simple activity that I enjoy very much every Saturday and Sunday".Split(' ');
        return string.Join(' ', Enumerable.Range(0, minimum).Select(index => words[index % words.Length]));
    }

    /// <summary>
    /// Submits items until the test reports completion. <paramref name="mcqChoice"/> maps a
    /// multiple-choice item element to the display option index to send (default: always 0).
    /// </summary>
    public static async Task RunToCompletionAsync(
        HttpClient client,
        Guid sessionId,
        JsonElement firstItem,
        Func<JsonElement, int>? mcqChoice = null)
    {
        mcqChoice ??= _ => 0;

        var item = firstItem;
        var guard = 0;
        while (item.ValueKind != JsonValueKind.Null && guard++ < 100)
        {
            var response = await SubmitAsync(client, sessionId, item, mcqChoice);
            if (response.GetProperty("isTestCompleted").GetBoolean())
                return;

            item = response.GetProperty("nextItem");
        }
    }

    private static async Task<JsonElement> SubmitAsync(
        HttpClient client, Guid sessionId, JsonElement item, Func<JsonElement, int> mcqChoice)
    {
        var kind = item.GetProperty("kind").GetInt32();
        var response = kind switch
        {
            WritingKind => await client.PostAsJsonAsync(
                "/api/placement/answer/writing",
                new { sessionId, taskId = item.GetProperty("id").GetGuid(), text = WeakWritingAnswer(item) }),
            SpeakingKind => await client.PostAsJsonAsync(
                "/api/placement/answer/speaking",
                new { sessionId, taskId = item.GetProperty("id").GetGuid(), audioContent = WeakSpeakingAudio }),
            _ => await client.PostAsJsonAsync(
                "/api/placement/answer",
                new { sessionId, questionId = item.GetProperty("id").GetGuid(), selectedOptionIndex = mcqChoice(item) }),
        };

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
