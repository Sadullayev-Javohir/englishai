using Application.Vocabulary.Admin;
using MediatR;

namespace Application.Vocabulary.Admin.GetVocabularyTopic;

public sealed record GetVocabularyTopicAdminQuery(Guid RequestingUserId, Guid Id)
    : IRequest<VocabularyTopicAdminDto>;
