using MediatR;

namespace Application.Developer.RevokeApiKey;

public sealed record RevokeDeveloperApiKeyCommand(Guid UserId, Guid ApiKeyId) : IRequest;
