using MediatR;

namespace Application.Vocabulary.Admin.Images;

public sealed record VocabularyImageAdminDto(
    Guid TopicId,
    Guid ImageId,
    string TopicTitle,
    string Level,
    string Word,
    string Translation,
    string ImageUrl,
    string? ImageSource,
    string? ImageAttribution,
    long Version);

public sealed record GetVocabularyImagesAdminQuery(Guid RequestingUserId)
    : IRequest<IReadOnlyList<VocabularyImageAdminDto>>;

public sealed record ReplaceVocabularyImageAdminCommand(
    Guid RequestingUserId,
    Guid TopicId,
    Guid ImageId)
    : IRequest<VocabularyImageAdminDto>;

public interface IVocabularyImageReplacementService
{
    Task<VocabularyImageAdminDto> ReplaceAsync(
        Guid topicId,
        Guid imageId,
        CancellationToken cancellationToken);
}
