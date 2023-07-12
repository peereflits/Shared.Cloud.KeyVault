using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Peereflits.Shared.Cloud.KeyVault.Tests;

public class AzureCertificatesTests
{
    private readonly AzureCertificates subject;

    public AzureCertificatesTests() => subject = new AzureCertificates(
                                                                       "my-key-vault-to-test",
                                                                       Substitute.For<ISecrets>(),
                                                                       Substitute.For<ILogger<AzureCertificates>>()
                                                                      );

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task WhenGet_WithoutAName_ItShouldThrow(string name)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => subject.Get(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task WhenGetWithPrivateKey_WithoutAName_ItShouldThrow(string name)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => subject.GetWithPrivateKey(name));
    }

    [Fact]
    public async Task WhenInstallingACertificateWitEmptyCertificate_ItShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => subject.Install("cert", "pass", Array.Empty<byte>()));
    }

    [Fact]
    public async Task WhenInstallingACertificateWithoutACertificate_ItShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => subject.Install("cert", "pass", null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task WhenDeletingWithoutAName_ItShouldThrow(string name)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => subject.Delete(name));
    }
}