using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Peereflits.Shared.Cloud.KeyVault.Tests.Helpers;
using Xunit;

namespace Peereflits.Shared.Cloud.KeyVault.Tests;

[Trait(TestTraits.Category, TestTraits.Integration)]
public class AzureCertificatesIntegrationTests : IClassFixture<IntegrationTestsFixture>
{
    private readonly IntegrationTestsFixture fixture;
    private readonly AzureCertificates subject;

    public AzureCertificatesIntegrationTests(IntegrationTestsFixture fixture)
    {
        this.fixture = fixture;

        ISecrets secrets = new AzureSecrets(fixture.Settings.KeyVaultName, NullLogger<AzureSecrets>.Instance);
        subject = new AzureCertificates(fixture.Settings.KeyVaultName, secrets, NullLogger<AzureCertificates>.Instance);
    }

    [Fact]
    public async Task WhenGet_ItShouldReturnACertificateWithoutPrivateKey()
    {
        await InstallTestCertificate("integrationTest-add");

        using X509Certificate2 result = await subject.Get("integrationTest-add");

        Assert.NotNull(result);
        Assert.Equal(fixture.Expected.Thumbprint, result.Thumbprint);
        Assert.False(result.HasPrivateKey);
    }

    [Fact]
    public async Task WhenGet_WhileNotExists_ItShouldThrow()
    {
        await Assert.ThrowsAsync<CertificateNotFoundException>(() => subject.Get("ThisIsANonExistingCertificate"));
    }

    [Fact]
    public async Task WhenGetWithPrivateKey_ItShouldReturnACertificatePair()
    {
        await InstallTestCertificate("integrationTest-get");

        using X509Certificate2 result = await subject.GetWithPrivateKey("integrationTest-get");

        Assert.NotNull(result);
        Assert.Equal(fixture.Expected.Thumbprint, result.Thumbprint);
        Assert.True(result.HasPrivateKey);
    }

    [Fact]
    public async Task WhenGetWithPrivateKey_WhileNotExists_ItShouldThrow()
    {
        await Assert.ThrowsAsync<CertificateNotFoundException>(() => subject.GetWithPrivateKey("ThisIsANonExistingCertificate"));
    }

    [Fact]
    public async Task WhenDelete_ItShouldRemoveCertificate()
    {
        await InstallTestCertificate("integrationTest-delete");

        await subject.Delete("integrationTest-delete");

        // Assert
        try
        {
            _ = await subject.Get("integrationTest-delete");
        }
        catch(CertificateNotFoundException)
        {
            return; // This is expected //
        }
        Assert.Fail("Certificate was not deleted");
    }

    [Fact]
    public async Task WhenDelete_WhileNotExists_ItShouldFailSilently()
    {
        await subject.Delete("ThisIsANonExistingCertificate");
    }


    private async Task InstallTestCertificate(string name)
    {
        await subject.Install(name, IntegrationTestsFixture.TestPassPhrase, fixture.TestCertificateContent);

        using X509Certificate2 result = await subject.GetWithPrivateKey(name);

        Assert.Equal(fixture.Expected.Thumbprint, result.Thumbprint);
        Assert.NotNull(result.GetRSAPrivateKey());
    }
}