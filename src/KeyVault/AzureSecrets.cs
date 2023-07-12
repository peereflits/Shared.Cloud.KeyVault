using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace Peereflits.Shared.Cloud.KeyVault;

internal class AzureSecrets : ISecrets
{
    private readonly SecretClient secretClient;
    private readonly ILogger<AzureSecrets> logger;

    public AzureSecrets(string keyVaultName, ILogger<AzureSecrets> logger)
    {
        secretClient = CreateSecretClient(keyVaultName);
        this.logger = logger;
    }

    public string Get(string name)
    {
        logger.LogInformation($"Executing {nameof(Get)} with '{{@SecretName}}'.", name);

        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException($"Parameter '{name}' is mandatory. It can not be empty of white space.",  nameof(name));
        }

        try
        {
            Response<KeyVaultSecret> secret = secretClient.GetSecret(name.Trim());
            logger.LogInformation($"Executed {nameof(Get)} with '{{@SecretName}}'.", name);

            return secret.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "SecretNotFound")
        {
            throw new SecretNotFoundException(name);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, $"Failed to execute {nameof(Get)} with '{{@SecretName}}'.", name);
            throw new KeyVaultException($"Failed to retrieve secret with name '{name}'.", ex);
        }
    }

    public async Task<string> GetAsync(string name)
    {
        logger.LogInformation($"Executing {nameof(GetAsync)} with '{{@SecretName}}'.", name);

        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException($"Parameter '{name}' is mandatory. It can not be empty of white space.",  nameof(name));
        }

        try
        {
            Response<KeyVaultSecret> secret = await secretClient.GetSecretAsync(name.Trim());
            logger.LogInformation($"Executed {nameof(GetAsync)} with '{{@SecretName}}'.", name);

            return secret.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "SecretNotFound")
        {
            throw new SecretNotFoundException(name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to execute {nameof(GetAsync)} with '{{@SecretName}}'.", name);
            throw new KeyVaultException($"Failed to retrieve secret with name '{name}'.", ex);
        }
    }

    private static SecretClient CreateSecretClient(string keyVaultName)
    {
        var uri = keyVaultName.ToLowerInvariant().StartsWith("https://")
                ? new Uri(keyVaultName)
                : new Uri($"https://{keyVaultName}.vault.azure.net");
        
        return new SecretClient(uri, new AzureCredentialBuilder().Build());
    }
}