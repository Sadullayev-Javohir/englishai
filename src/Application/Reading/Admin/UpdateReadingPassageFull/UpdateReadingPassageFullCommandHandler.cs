using Application.Common;
using Application.Identity.Dtos;
using Application.Reading.Ports;
using Domain.Assessment;
using Domain.Reading;
using MediatR;

namespace Application.Reading.Admin.UpdateReadingPassageFull;

public sealed class UpdateReadingPassageFullCommandHandler
    : IRequestHandler<UpdateReadingPassageFullCommand, ReadingPassageAdminDetailDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IReadingRepository _passages;

    public UpdateReadingPassageFullCommandHandler(IAdminAuthorization admin, IReadingRepository passages)
    {
        _admin = admin;
        _passages = passages;
    }

    public async Task<ReadingPassageAdminDetailDto> Handle(
        UpdateReadingPassageFullCommand request, CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var passage = await _passages.GetByIdAsync(request.Id, cancellationToken)
                      ?? throw new NotFoundException(nameof(ReadingPassage), request.Id);
        var payload = request.Payload;
        var glossary = payload.Vocabulary.Select(item =>
            GlossaryEntry.Create(item.Word, item.Translation, item.ExampleSentence)).ToList();
        var questions = payload.Questions.Select(item =>
            ReadingQuestion.Create(
                item.Prompt,
                item.Options,
                item.CorrectOptionIndex,
                item.HintCode,
                item.Explanation)).ToList();

        passage.AdminReplaceContent(
            payload.Title,
            payload.Topic,
            payload.Category,
            Enum.Parse<CefrLevel>(payload.Level, true),
            Enum.Parse<ReadingPassageStatus>(payload.Status, true),
            string.Join("\n\n", payload.Sections.Select(section => section.Trim()).Where(section => section.Length > 0)),
            payload.ContextGaps,
            glossary,
            questions);

        await _passages.SaveAsync(passage, cancellationToken);
        return ReadingPassageAdminDetailDto.FromDomain(passage);
    }
}
