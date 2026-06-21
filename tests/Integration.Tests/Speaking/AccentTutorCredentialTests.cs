using Azure.Identity;
using FluentAssertions;
using Infrastructure.Speaking;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Speaking;

/// <summary>
/// The live Accent Tutor authenticates against Azure AI Foundry. A configured service principal
/// (TenantId + ClientId + ClientSecret, supplied via user-secrets in dev or env/.env in prod) must
/// select an explicit ClientSecretCredential so token acquisition works in both environments; when
/// absent it falls back to the ambient identity (managed identity / az login). Getting this wrong is
/// what left every accent tutor recognising speech but never replying (accent_tutor_authentication_failed).
/// </summary>
public class AccentTutorCredentialTests
{
    [Fact]
    public void Service_principal_selects_client_secret_credential()
    {
        var options = new AzureAccentTutorOptions
        {
            Enabled = true,
            TenantId = "11111111-1111-1111-1111-111111111111",
            ClientId = "22222222-2222-2222-2222-222222222222",
            ClientSecret = "test-secret",
        };

        options.HasServicePrincipal.Should().BeTrue();
        AzureAccentTutorAgent.CreateCredential(options, NullLogger<AzureAccentTutorAgent>.Instance)
            .Should().BeOfType<ClientSecretCredential>();
    }

    [Theory]
    [InlineData("", "cid", "secret")]
    [InlineData("tid", "", "secret")]
    [InlineData("tid", "cid", "")]
    public void Incomplete_service_principal_falls_back_to_default_credential(
        string tenantId, string clientId, string clientSecret)
    {
        var options = new AzureAccentTutorOptions
        {
            Enabled = true,
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecret = clientSecret,
        };

        options.HasServicePrincipal.Should().BeFalse();
        AzureAccentTutorAgent.CreateCredential(options, NullLogger<AzureAccentTutorAgent>.Instance)
            .Should().BeOfType<DefaultAzureCredential>();
    }
}
