using Application.Common;
using Application.Developer.Ports;
using Domain.Developer;
using MediatR;

namespace Application.Developer.RevokeApiKey;

public sealed class RevokeDeveloperApiKeyCommandHandler : IRequestHandler<RevokeDeveloperApiKeyCommand>
{
    private readonly IDeveloperApiKeyStore _keys;
    private readonly TimeProvider _clock;

    public RevokeDeveloperApiKeyCommandHandler(IDeveloperApiKeyStore keys, TimeProvider clock)
    {
        _keys = keys;
        _clock = clock;
    }

    public async Task Handle(RevokeDeveloperApiKeyCommand request, CancellationToken cancellationToken)
    {
        var apiKey = await _keys.GetByIdAsync(request.ApiKeyId, cancellationToken)
            ?? throw new NotFoundException(nameof(DeveloperApiKey), request.ApiKeyId);

        if (apiKey.UserId != request.UserId)
            throw new NotFoundException(nameof(DeveloperApiKey), request.ApiKeyId);

        apiKey.Revoke(_clock.GetUtcNow());
        await _keys.UpdateAsync(apiKey, cancellationToken);
    }
}
