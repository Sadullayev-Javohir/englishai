namespace Application.Translation.Dtos;

/// <summary>
/// The Uzbek translation of one English sentence/text, returned to the on-demand sentence-translation
/// UI (clicking or hovering a sentence in any English teaching text). <see cref="Translation"/> is
/// null when translation is unavailable, so the UI shows an honest "unavailable" state (rules 8, 11).
/// </summary>
public sealed record TranslationDto(string Text, string? Translation);
