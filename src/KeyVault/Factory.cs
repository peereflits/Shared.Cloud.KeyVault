using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Peereflits.Shared.Cloud.KeyVault;

public class Factory : IFactory
{
    private readonly string keyVaultName;
    private readonly ILoggerFactory loggerFactory;

    internal Factory(string keyVaultName, ILoggerFactory loggerFactory)
    {
        this.keyVaultName = keyVaultName;
        this.loggerFactory = loggerFactory;
    }

    public ISecrets CreateSecrets() 
        => new AzureSecrets(keyVaultName, loggerFactory.CreateLogger<AzureSecrets>());

    public ICertificates CreateCertificates()
    {
        ISecrets secret = new AzureSecrets(keyVaultName, loggerFactory.CreateLogger<AzureSecrets>());
        return new AzureCertificates(keyVaultName, secret, loggerFactory.CreateLogger<AzureCertificates>());
    }

    public static IFactory CreateFactory(string keyVaultName, ILoggerFactory loggerFactory)
    {
        if(string.IsNullOrWhiteSpace(keyVaultName))
        {
            throw new ArgumentNullException(nameof(keyVaultName), $"Parameter '{nameof(keyVaultName)}' is mandatory. It can not be empty of white space.");
        }
        if(loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory), $"Parameter '{nameof(loggerFactory)}' is mandatory. It can not be nothing.");
        }
        
        return new Factory(keyVaultName, loggerFactory);
    }

    public static ISecrets CreateSecretWithoutLogging(string keyVaultName) 
        => CreateFactory(keyVaultName, NullLoggerFactory.Instance).CreateSecrets();

    public static ICertificates CreateCertificatesWithoutLogging(string keyVaultName) 
        => CreateFactory(keyVaultName, NullLoggerFactory.Instance).CreateCertificates();
}