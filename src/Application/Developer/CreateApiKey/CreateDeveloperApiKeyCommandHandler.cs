using Application.Common;
using Application.Developer.Dtos;
using Application.Developer.Ports;
using Application.Identity.Ports;
using Domain.Developer;
using Domain.Identity;
using MediatR;

namespace Application.Developer.CreateApiKey;

public sealed class CreateDeveloperApiKeyCommandHandler
    : IRequestHandler<CreateDeveloperApiKeyCommand, CreatedDeveloperApiKeyDto>
{
    public const int MaxActiveKeysPerUser = 5;

    private readonly IUserAccountStore _accounts;
    private readonly IDeveloperApiKeyStore _keys;
    private readonly IDeveloperApiKeyProtector _protector;
    private readonly TimeProvider _clock;

    public CreateDeveloperApiKeyCommandHandler(
        IUserAccountStore accounts,
        IDeveloperApiKeyStore keys,
        IDeveloperApiKeyProtector protector,
        TimeProvider clock)
    {
        _accounts = accounts;
        _keys = keys;
        _protector = protector;
        _clock = clock;
    }

    public async Task<CreatedDeveloperApiKeyDto> Handle(
        CreateDeveloperApiKeyCommand request,
        CancellationToken cancellationToken)
    {
        _ = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);

        var existing = await _keys.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existing.Count(k => k.IsActive) >= MaxActiveKeysPerUser)
            throw new ConflictException($"An account can have at most {MaxActiveKeysPerUser} active API keys.");

        var generated = _protector.Generate();
        var now = _clock.GetUtcNow();
        var apiKey = DeveloperApiKey.Create(
            request.UserId,
            request.Name,
            generated.Prefix,
            generated.Hash,
            now);

        await _keys.AddAsync(apiKey, cancellationToken);

        return new CreatedDeveloperApiKeyDto(
            apiKey.Id,
            apiKey.Name,
            apiKey.Prefix,
            generated.Plaintext,
            apiKey.CreatedAt);
    }
}
