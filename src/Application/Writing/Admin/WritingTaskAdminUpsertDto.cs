namespace Application.Writing.Admin;

/// <summary>
/// Create/edit payload for a writing task. Only the CEFR <see cref="Level"/> is supplied - the word
/// range is derived from <c>WritingWordRange.For(level)</c> on the server.
/// </summary>
public sealed record WritingTaskAdminUpsertDto(string Level);
