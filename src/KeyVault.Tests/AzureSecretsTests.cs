using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Peereflits.Shared.Cloud.KeyVault.Tests;

public class AzureSecretsTests
{
    private readonly AzureSecrets subject;

    public AzureSecretsTests()
    {
        var logger = Substitute.For<ILogger<AzureSecrets>>();
        subject = new AzureSecrets("my-key-vault-to-test", logger);
    }

    [Theory]
    [InlineData(default(string))]
    [InlineData("")]
    [InlineData("\r\n")]
    public async Task WhenGetAsync_WithoutAName_ItShouldThrow(string name)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => subject.GetAsync(name));
    }

    [Theory]
    [InlineData(default(string))]
    [InlineData("")]
    [InlineData("\r\n")]
    public void WhenGet_WithoutAName_ItShouldThrow(string name)
    {
        Assert.Throws<ArgumentNullException>(() => subject.Get(name));
    }
}