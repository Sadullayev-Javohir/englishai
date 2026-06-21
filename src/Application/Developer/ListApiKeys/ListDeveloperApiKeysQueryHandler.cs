using Application.Developer.Dtos;
using Application.Developer.Ports;
using MediatR;

namespace Application.Developer.ListApiKeys;

public sealed class ListDeveloperApiKeysQueryHandler
    : IRequestHandler<ListDeveloperApiKeysQuery, IReadOnlyList<DeveloperApiKeyDto>>
{
    private readonly IDeveloperApiKeyStore _keys;

    public ListDeveloperApiKeysQueryHandler(IDeveloperApiKeyStore keys) => _keys = keys;

    public async Task<IReadOnlyList<DeveloperApiKeyDto>> Handle(
        ListDeveloperApiKeysQuery request,
        CancellationToken cancellationToken) =>
        (await _keys.GetByUserIdAsync(request.UserId, cancellationToken))
            .Select(DeveloperApiKeyDto.From)
            .ToList();
}
