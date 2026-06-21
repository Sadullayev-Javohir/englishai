using Domain.Books;

namespace Application.Books.Dtos;

public sealed record AdminBookQuestionDto(Guid Id, string Prompt, IReadOnlyList<string> Options,
    int CorrectOptionIndex, string? Explanation)
{
    public static AdminBookQuestionDto FromDomain(BookQuestion q) =>
        new(q.Id, q.Prompt, q.Options, q.CorrectOptionIndex, q.Explanation);
}

public sealed record AdminBookSectionDto(Guid Id, int Order, string Title, string Status, string Body,
    IReadOnlyList<AdminBookQuestionDto> Questions)
{
    public static AdminBookSectionDto FromDomain(BookSection s) =>
        new(s.Id, s.Order, s.Title, s.Status.ToString(), s.Body, s.Questions.Select(AdminBookQuestionDto.FromDomain).ToArray());
}

public sealed record AdminBookDto(Guid Id, string Title, string TitleUz, string Author, string Synopsis,
    string Topic, string Level, string CoverImageQuery, string? CoverImageUrl, string? CoverAttribution,
    IReadOnlyList<AdminBookSectionDto> Sections)
{
    public static AdminBookDto FromDomain(Book b) => new(b.Id,b.Title,b.TitleUz,b.Author,b.Synopsis,b.Topic,b.Level.ToString(),
        b.CoverImageQuery,b.CoverImageUrl,b.CoverAttribution,b.Sections.Select(AdminBookSectionDto.FromDomain).ToArray());
}
