using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Peereflits.Shared.Cloud.KeyVault.Tests.Helpers;
using Xunit;

namespace Peereflits.Shared.Cloud.KeyVault.Tests;

[Trait(TestTraits.Category, TestTraits.Integration)]
public class AzureSecretsIntegrationTests : IClassFixture<IntegrationTestsFixture>
{
    private readonly AzureSecrets subject;

    public AzureSecretsIntegrationTests(IntegrationTestsFixture fixture) 
        => subject = new AzureSecrets(fixture.Settings.KeyVaultName, NullLogger<AzureSecrets>.Instance);

    [Fact]
    public async Task WhenGetAsync_ItShouldReturnItsValue()
    {
        string result = await subject.GetAsync("IntegrationTest-Secret");

        Assert.Equal("This is secret", result);
    }

    [Fact]
    public async Task WhenGetAsync_WhileNotExists_ItShouldThrow()
    {
        await Assert.ThrowsAsync<SecretNotFoundException>(() => subject.GetAsync("a-non-existing-key"));
    }

    [Fact]
    public void WhenGet_ItShouldReturnItsValue()
    {
        string result = subject.Get("IntegrationTest-Secret");

        Assert.Equal("This is secret", result);
    }

    [Fact]
    public void WhenGetSecret_NonExisting_ItShouldThrow()
    {
        Assert.Throws<SecretNotFoundException>(() => subject.Get("a-non-existing-key"));
    }
}