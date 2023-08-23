![Logo](./img/peereflits-logo.png) 

# Peereflits.Shared.Cloud.KeyVault

Azure KeyVault protects digital securables used by cloud apps and services. It plays a key role in secure applications because it makes passwords (secrets) in source code and configuration files superfluous, among other things. Its protection includes "Keys", "Secrets" and "Certificates". See e.g.:
* https://azure.microsoft.com/en-us/products/key-vault/
* https://learn.microsoft.com/en-us/azure/key-vault/

`Peereflits.Shared.Cloud.KeyVault` is a wrapper library on top of the [Azure Key Vault libraries for .NET
](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/key-vault?view=azure-dotnet) that allows reading secrets and managing certificates (read, add, delete). In addition, there are also "keys" (cryptographic RSA/EC keys); these are not supported in this library.

The purpose of *Peereflits.Shared.Cloud.KeyVault* is:
1. [Simplicity of usage](#Usage)
1. [Management of authentication credentials](#Management%20of%20authentication%20credentials)
1. Handling transient errors<br/>This is build into the classes `AzureCertificates` and `AzureSecrets`;
1. Logging interaction with KeyVault<br/>See the `Factory` class and [usage](#Usage) below;
1. Optimize performance<br/>See [Performance optimizations](#Performance%20optimizations)


## KeyVault packages, dependencies & class diagram

`Peereflits.Shared.Cloud.KeyVault` consists of two packages:
1. `Peereflits.Shared.Cloud.KeyVault.Interfaces`
    * Dependencies: none
    * contains interfaces, DTOs, exceptions
1. `Peereflits.Shared.Cloud.KeyVault`
    * Implements `KeyVault.Interfaces`
    * Has dependencies on:
       * `Azure.Identity` for Authentication Credential Management
       * `Azure.Security.KeyVault.Certificates` for Certificates
       * `Azure.Security.KeyVault.Secrets` for Secrets
       * `Microsoft.Extensions.Logging.Abstractions` for Logging

See the class diagram below for an overview of the most important classes and interfaces.

[![](https://mermaid.ink/img/pako:eNqdk99LwzAQx_-VkKcNtiGCD5Y5kQ1liDCYoGD3cEuvWzBNR3JVytz-drOm2xotCOalvV-f7-WSbLnIE-QRFwqsnUhYGchizdyqPGw6R2GQ7NY7D2s4lJrQpCBwNDq7H5A6lozUK6Yhwy7zRpBwZ0stwqxnsO9779n73F2gP0ZDMpUCCP_TRIV_vbq4bnAu90HBi6T1zMgPF3nEslneq_dwyzaul8_cJOyG6UKpv7lTbQmUaqNVsNnagHWuZUn4tmDiTPHsM2mCCglbdrXMc9U6snsQlJvyNK3WWY0NOjF_up3u6ZzDcHP4h6SmHSgfNdnwq99ntVGHarIPBTIBz8d_KxzJg8HoR5dBJCzkPZ6hyUAm7m5Xg4g5rTHDmEfuN8EUCkUxj_XOpUJB-dzdTB6RKbDHi03iMPVr4FEKyh0Vx0Q6uaf6vRw-u28yERDD?type=png)](https://mermaid.live/edit#pako:eNqdk99LwzAQx_-VkKcNtiGCD5Y5kQ1liDCYoGD3cEuvWzBNR3JVytz-drOm2xotCOalvV-f7-WSbLnIE-QRFwqsnUhYGchizdyqPGw6R2GQ7NY7D2s4lJrQpCBwNDq7H5A6lozUK6Yhwy7zRpBwZ0stwqxnsO9779n73F2gP0ZDMpUCCP_TRIV_vbq4bnAu90HBi6T1zMgPF3nEslneq_dwyzaul8_cJOyG6UKpv7lTbQmUaqNVsNnagHWuZUn4tmDiTPHsM2mCCglbdrXMc9U6snsQlJvyNK3WWY0NOjF_up3u6ZzDcHP4h6SmHSgfNdnwq99ntVGHarIPBTIBz8d_KxzJg8HoR5dBJCzkPZ6hyUAm7m5Xg4g5rTHDmEfuN8EUCkUxj_XOpUJB-dzdTB6RKbDHi03iMPVr4FEKyh0Vx0Q6uaf6vRw-u28yERDD)

## Usage

`Peereflits.Shared.Cloud.KeyVault` can be used both synchronously and asynchronously (see the class diagram) in DI-enabled scenarios as well as in scenarios where DI is not available.

### Usage in DI enabled scenarios

For use in DI-enabled scenarios (read: in ASP&#46;NET applications) the DI-container can be configured as follows:

``` csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddScoped<ISecrets>(sp =>
{
    IFactory factory = Factory.CreateFactory("my-keyvault-name", sp.GetRequiredService<ILoggerFactory>());
    return factory.CreateSecrets();
});
builder.Services.AddScoped<ICertificates>(sp =>
{
    IFactory factory = Factory.CreateFactory("my-keyvault-name", sp.GetRequiredService<ILoggerFactory>());
    return factory.CreateCertificates();
});

// Other services to add
        
var app = builder.Build();
Configure(app);
app.Run();

```

In the other projects other than the host (i.e. in the domain layer and others) only a dependency on `Peereflits.Shared.Cloud.KeyVault.Interfaces` is needed. Then an `ISecrets` or an `ICertificates` can then be injected into the constructor of implementing classes.

> **Note**
> Working with and configuring `Peereflits.Shared.Dependencies` is described [here](https://github.com/peereflits/Shared.Dependencies).

### Usage in DI disabled scenarios

In scenarios where DI is not available, you can use the `Factory` like this:

``` csharp
IFactory factory = Factory.CreateFactory("my-keyvault-name", new LoggerFactory());
ISecrets secrets = factory.CreateSecrets();
```

Or in cases where logging is not needed (for example in unit tests), the `Factory` can be used as follows:

``` csharp
ISecrets secrets = Factory.CreateSecretsWithoutLogging("my-keyvault-name");
string secret = await secrets.Get("MySecretName");
```

## Management of authentication credentials

Authentication (and authorization) of users and applications is managed in the KeyVault itself by access policies or RBAC configuration. This library does not change that.

The validation and authentication of credentials is managed and executed by [Azure.Identity.DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet). There are eight credential providers available in `DefaultAzureCredential`, of which three can be conditionally enabled.

The following authentication credentials is automatically set as follows:

1. **Environment Credential**: when the environment variables `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET` and `AZURE_TENANT_ID` are set. This may apply in building and release pipelines, among others;
1. **Managed Identity Credential**: when the application is hosted by an Azure WebApp and a [System Assigned Identity](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) has been assinged;
1. **Visual Studio Credential**: when the application is running within Visual Studio. This is applied when Environment and Managed Identity Credentials are not available.<br/>**Note:** Make sure that the credentials of the developer are set correctly via Tools | Options | Azure Service Authentication;
1. Azure CLI Credential: not supported;
1. Azure PowerShell Credential: not supported;
1. Interactive Browser Credential: not supported;
1. Shared Token Cache Credential: not supported;
1. Visual Studio Code Credential: not supported.

Details of authentication credential configuration can be found in class `AzureCredentialBuilder`.

## Performance optimizations

The performance of the (http-)clients in the `Azure.Security.KeyVault` SDK is not subject to performance issues. 
The biggest performance hog is the usage of (an unconfigured) `DefaultAzureCredential` as it uses a fall-through policy by trying several credential types untill it succeeds.
Due to the optimization in the `AzureCredentialBuilder` (see above) a performance benefit has been achieved.

---

<p align="center">
&copy; No copyright applicable<br />
&#174; "Peereflits" is my codename.
</p>

---
