using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Peereflits.Shared.Cloud.KeyVault.Tests;

public class FactoryTests
{
    private readonly ILoggerFactory loggerFactory;
    private readonly Factory subject;

    public FactoryTests()
    {
        loggerFactory = Substitute.For<ILoggerFactory>();
        subject = new Factory("my-key-vault-to-test", loggerFactory);
    }

    [Fact]
    public void WhenCreateFactory_ItShouldSucceed()
    {
        IFactory result = Factory.CreateFactory("my-key-vault-to-test", loggerFactory);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("\r\n")]
    public void WhenCreateFactory_WithoutName_ItShouldThrow(string? keyVaultName)
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Factory.CreateFactory(keyVaultName ?? string.Empty, NullLoggerFactory.Instance));
        Assert.Equal("keyVaultName", ex.ParamName);
    }

    [Fact]
    public void WhenCreateSecrets_ItShouldSucceed()
    {
        ISecrets result = subject.CreateSecrets();
        Assert.NotNull(result);
    }

    [Fact]
    public void WhenCreateCertificates_ItShouldSucceed()
    {
        ICertificates result = subject.CreateCertificates();
        Assert.NotNull(result);
    }
}