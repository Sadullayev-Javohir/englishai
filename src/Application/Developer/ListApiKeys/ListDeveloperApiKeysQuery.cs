using Application.Developer.Dtos;
using MediatR;

namespace Application.Developer.ListApiKeys;

public sealed record ListDeveloperApiKeysQuery(Guid UserId)
    : IRequest<IReadOnlyList<DeveloperApiKeyDto>>;
