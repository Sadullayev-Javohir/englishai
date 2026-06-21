using Application.Common;
using Application.Developer.CreateApiKey;
using Application.Developer.ListApiKeys;
using Application.Developer.Ports;
using Application.Developer.RevokeApiKey;
using Application.Identity.Ports;
using Domain.Developer;
using Domain.Identity;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Developer;

public sealed class DeveloperApiKeyHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 21, 10, 0, 0, TimeSpan.Zero);
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly IDeveloperApiKeyStore _keys = Substitute.For<IDeveloperApiKeyStore>();
    private readonly IDeveloperApiKeyProtector _protector = Substitute.For<IDeveloperApiKeyProtector>();
    private readonly TimeProvider _clock = new FixedClock(Now);

    [Fact]
    public async Task Create_returns_plaintext_once_and_persists_only_hash()
    {
        var account = UserAccount.Register("sub", "dev@example.com", "Dev", null, Now);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _keys.GetByUserIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<DeveloperApiKey>());
        _protector.Generate().Returns(new GeneratedDeveloperApiKey("eai_plaintext", "eai_plaintex", "HASH"));

        var result = await new CreateDeveloperApiKeyCommandHandler(_accounts, _keys, _protector, _clock)
            .Handle(new CreateDeveloperApiKeyCommand(account.Id, "CLI"), CancellationToken.None);

        result.ApiKey.Should().Be("eai_plaintext");
        await _keys.Received(1).AddAsync(
            Arg.Is<DeveloperApiKey>(k => k.KeyHash == "HASH" && k.Prefix == "eai_plaintex"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_rejects_more_than_five_active_keys()
    {
        var account = UserAccount.Register("sub", "dev@example.com", "Dev", null, Now);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _keys.GetByUserIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(
            Enumerable.Range(0, 5).Select(i => DeveloperApiKey.Create(
                account.Id, $"Key {i}", $"eai_{i:00000000}", new string((char)('A' + i), 64), Now)).ToList());

        var act = () => new CreateDeveloperApiKeyCommandHandler(_accounts, _keys, _protector, _clock)
            .Handle(new CreateDeveloperApiKeyCommand(account.Id, "Sixth"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _keys.DidNotReceive().AddAsync(Arg.Any<DeveloperApiKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Revoke_hides_foreign_key_as_not_found()
    {
        var key = DeveloperApiKey.Create(Guid.NewGuid(), "CLI", "eai_12345678", new string('A', 64), Now);
        _keys.GetByIdAsync(key.Id, Arg.Any<CancellationToken>()).Returns(key);

        var act = () => new RevokeDeveloperApiKeyCommandHandler(_keys, _clock)
            .Handle(new RevokeDeveloperApiKeyCommand(Guid.NewGuid(), key.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task List_returns_only_requested_users_keys_from_store()
    {
        var userId = Guid.NewGuid();
        _keys.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new[]
        {
            DeveloperApiKey.Create(userId, "CLI", "eai_12345678", new string('A', 64), Now)
        });

        var result = await new ListDeveloperApiKeysQueryHandler(_keys)
            .Handle(new ListDeveloperApiKeysQuery(userId), CancellationToken.None);

        result.Should().ContainSingle().Which.Name.Should().Be("CLI");
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
