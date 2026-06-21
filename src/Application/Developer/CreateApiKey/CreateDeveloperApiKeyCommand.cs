using Application.Developer.Dtos;
using MediatR;

namespace Application.Developer.CreateApiKey;

public sealed record CreateDeveloperApiKeyCommand(Guid UserId, string Name)
    : IRequest<CreatedDeveloperApiKeyDto>;
