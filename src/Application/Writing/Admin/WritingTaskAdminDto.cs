using Domain.Writing;

namespace Application.Writing.Admin;

/// <summary>
/// A writing task row for the admin catalog (PROJECT-SPEC G.3) - the curated level, status, computed
/// word range, prompt snippet and link to the owning vocabulary topic. Distinct from any learner
/// view, which layers submission progress on top.
/// </summary>
public sealed record WritingTaskAdminDto(
    Guid Id,
    string Level,
    string Status,
    int MinWords,
    int MaxWords,
    string PromptSnippet,
    Guid? VocabularyTopicId,
    DateTimeOffset CreatedAt)
{
    /// <summary>Builds the admin row from the aggregate (status/level as human-readable strings).</summary>
    public static WritingTaskAdminDto FromDomain(WritingTask task) =>
        new(
            task.Id,
            task.Level.ToString(),
            task.Status.ToString(),
            task.MinWords,
            task.MaxWords,
            Snippet(task.Prompt),
            task.VocabularyTopicId,
            task.CreatedAt);

    /// <summary>First ~90 characters of the prompt, or "" if the prompt is empty.</summary>
    private static string Snippet(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
            return string.Empty;

        return prompt.Length <= 90 ? prompt : prompt[..90];
    }
}
