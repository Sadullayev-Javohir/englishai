using Application.Common;
using Application.Identity.Dtos;
using Application.Vocabulary.Admin;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.Admin.UpdateVocabularyTopic;

public sealed class UpdateVocabularyTopicCommandHandler
    : IRequestHandler<UpdateVocabularyTopicCommand, VocabularyTopicAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IVocabularyTopicRepository _topics;

    public UpdateVocabularyTopicCommandHandler(IAdminAuthorization admin, IVocabularyTopicRepository topics)
    {
        _admin = admin;
        _topics = topics;
    }

    public async Task<VocabularyTopicAdminDto> Handle(
        UpdateVocabularyTopicCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var topic = await _topics.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.Id);

        topic.UpdateMetadata(
            request.Title, request.TitleUz, request.Category, request.GrammarFocusCode, request.Sequence);

        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);
        topic.SetLevel(level);

        if (!string.IsNullOrWhiteSpace(request.Passage) || request.Words.Count > 0)
        {
            var words = request.Words.Select(word =>
            {
                var partOfSpeech = PartOfSpeechParser.Parse(word.PartOfSpeech);
                return TopicWord.Create(
                    word.Word,
                    word.Translation,
                    word.ExampleSentence,
                    partOfSpeech,
                    LexicalCategoryParser.Parse(word.LexicalCategory, partOfSpeech),
                    word.Register,
                    word.UsageNote,
                    word.ImageUrl,
                    word.ImageSource,
                    word.ImageAttribution);
            });
            topic.UpdateContent(request.Passage, words);
        }

        await _topics.SaveAsync(topic, cancellationToken);

        return VocabularyTopicAdminDto.FromDomain(topic);
    }
}
