using Domain.Reading;

namespace Application.Reading.Admin;

public sealed record ReadingGlossaryAdminDto(
    Guid Id,
    string Word,
    string Translation,
    string? ExampleSentence);

public sealed record ReadingQuestionAdminDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    string? HintCode,
    string? Explanation);

public sealed record ReadingPassageAdminDetailDto(
    Guid Id,
    string Title,
    string Topic,
    string Category,
    string Level,
    string Status,
    string Body,
    IReadOnlyList<string> Sections,
    string? ContextGaps,
    IReadOnlyList<ReadingGlossaryAdminDto> Vocabulary,
    IReadOnlyList<ReadingQuestionAdminDto> Questions,
    Guid? VocabularyTopicId,
    DateTimeOffset CreatedAt)
{
    public static ReadingPassageAdminDetailDto FromDomain(ReadingPassage passage) =>
        new(
            passage.Id,
            passage.Title,
            passage.Topic,
            passage.Category,
            passage.Level.ToString(),
            passage.Status.ToString(),
            passage.Body,
            SplitSections(passage.Body),
            passage.ContextGaps,
            passage.Glossary.Select(entry => new ReadingGlossaryAdminDto(
                entry.Id, entry.Word, entry.Translation, entry.ExampleSentence)).ToList(),
            passage.Questions.Select(question => new ReadingQuestionAdminDto(
                question.Id,
                question.Prompt,
                question.Options,
                question.CorrectOptionIndex,
                question.HintCode,
                question.Explanation)).ToList(),
            passage.VocabularyTopicId,
            passage.CreatedAt);

    private static IReadOnlyList<string> SplitSections(string body) =>
        body.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed record ReadingGlossaryAdminUpsertDto(
    string Word,
    string Translation,
    string? ExampleSentence);

public sealed record ReadingQuestionAdminUpsertDto(
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    string? HintCode,
    string? Explanation);

public sealed record ReadingPassageAdminFullUpsertDto(
    string Title,
    string Topic,
    string Category,
    string Level,
    string Status,
    IReadOnlyList<string> Sections,
    string? ContextGaps,
    IReadOnlyList<ReadingGlossaryAdminUpsertDto> Vocabulary,
    IReadOnlyList<ReadingQuestionAdminUpsertDto> Questions);
