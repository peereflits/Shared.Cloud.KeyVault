using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Peereflits.Shared.Cloud.KeyVault.Tests.Helpers;

public class IntegrationTestsFixture : IDisposable
{
    internal const string TestPassPhrase = "test";

    public IntegrationTestsFixture()
    {
        Settings = GetTestSettings();
        Environment.SetEnvironmentVariable("TENANT_ID", Settings.TenantId);

        TestCertificateContent = CreateTestCertificate();
        Expected = new X509Certificate2(TestCertificateContent, TestPassPhrase);
    }

    private static TestSettings GetTestSettings()
    {
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var builder = new ConfigurationBuilder()
                     .AddJsonFile("testsettings.json")
                     .AddJsonFile($"testsettings.{environment}.json", true)
                     .Build();

        var settings = new TestSettings();
        builder.GetSection(nameof(TestSettings)).Bind(settings);

        return settings;
    }

    private static byte[] CreateTestCertificate()
    {
        const string subject = "CN=certificate-test.peereflits.nl,OU=Development,O=Peereflits,L=Amsterdam,ST=NH,C=NL";
        var rsa = new RSACryptoServiceProvider(2048);
        var req = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

        Assert.True(cert.HasPrivateKey);

        return cert.Export(X509ContentType.Pfx, TestPassPhrase);
    }

    public TestSettings Settings { get; }

    public byte[] TestCertificateContent { get; }
    public X509Certificate2 Expected { get; }

    public void Dispose()
    {
        Expected?.Dispose();
    }
}