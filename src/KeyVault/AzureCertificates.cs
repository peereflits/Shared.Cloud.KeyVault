using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;

namespace Peereflits.Shared.Cloud.KeyVault;

internal class AzureCertificates : ICertificates
{
    private readonly ISecrets secrets;
    private readonly ILogger<AzureCertificates> logger;
    private readonly CertificateClient certificateClient;

    public AzureCertificates
    (
        string keyVaultName,
        ISecrets secrets,
        ILogger<AzureCertificates> logger
    )
    {
        this.secrets = secrets;
        certificateClient = CreateCertificateClient(keyVaultName);
        this.logger = logger;
    }

    public async Task<X509Certificate2> Get(string name)
    {
        logger.LogInformation($"Executing {nameof(Get)} with '{{@CertificateName}}'.", name);

        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), $"Parameter '{name}' is mandatory. It can not be empty of white space.");
        }

        try
        {
            Response<KeyVaultCertificateWithPolicy> policy = await certificateClient.GetCertificateAsync(name);
            var result = GetCertificate(policy.Value.Cer);
            logger.LogInformation($"Executed {nameof(Get)} with '{{@CertificateName}}'.", name);

            return result;
        }
        catch(RequestFailedException ex) when(ex.ErrorCode == "CertificateNotFound")
        {
            throw new CertificateNotFoundException(name);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, $"Failed to execute {nameof(Get)} with '{{@CertificateName}}'.", name);
            throw new KeyVaultException($"Failed to retrieve certificate with name '{name}'.", ex);
        }
    }

    public async Task<X509Certificate2> GetWithPrivateKey(string name, string? password = null)
    {
        logger.LogInformation($"Executing {nameof(GetWithPrivateKey)} with '{{@CertificateName}}'.", name);

        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), $"Parameter '{name}' is mandatory. It can not be empty of white space.");
        }

        try
        {
            string cert = await secrets.GetAsync(name);
            byte[] arr = Convert.FromBase64String(cert);

            X509Certificate2 result = GetCertificate(arr, password);

            logger.LogInformation($"Executed {nameof(GetWithPrivateKey)} with '{{@CertificateName}}'.", name);

            return result;
        }
        catch(SecretNotFoundException)
        {
            throw new CertificateNotFoundException(name);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, $"Failed to execute {nameof(GetWithPrivateKey)} with '{{@CertificateName}}'.", name);
            throw new KeyVaultException($"Failed to retrieve certificate with name '{name}'.", ex);
        }
    }

    private static X509Certificate2 GetCertificate(byte[] certificate, string? password = null)
    {
#if NET9_0_OR_GREATER
        var result = X509Certificate2.GetCertContentType(certificate) == X509ContentType.Pfx
                     ? X509CertificateLoader.LoadPkcs12(certificate, password)
                     : X509CertificateLoader.LoadCertificate(certificate);
#else
        var result = string.IsNullOrWhiteSpace(password)
                  ? new X509Certificate2(certificate)
                  : new X509Certificate2(certificate, password);
#endif
        return result;
    }

    public async Task Install(string name, string passPhrase, byte[] certificate)
    {
        logger.LogInformation($"Executing {nameof(Install)} with '{{@CertificateName}}'.", name);

        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), $"Parameter '{name}' is mandatory. It can not be empty of white space.");
        }

        if(!certificate.Any())
        {
            throw new ArgumentNullException(nameof(certificate), $"Parameter '{certificate}' is mandatory. It can not be empty.");
        }

        try
        {
            try
            {
                await certificateClient.ImportCertificateAsync(new ImportCertificateOptions(name, certificate)
                                                               {
                                                                   Password = passPhrase,
                                                                   Enabled = true
                                                               }
                                                              );
            }
            catch(RequestFailedException ex) when(ex.Status == 409)
            {
                // Certificate exists in a deleted state and can not be overwritten
                // so we recover the deleted certificate first and then try again
                await Reinstall(name, passPhrase, certificate!);
            }

            logger.LogInformation($"Executed {nameof(Install)} with '{{@CertificateName}}'.", name);
        }
        catch(RequestFailedException ex)
        {
            logger.LogError(ex, $"Failed to execute {nameof(Install)} with '{{@CertificateName}}'.", name);
            throw new KeyVaultException($"Failed to install certificate with name '{name}'.", ex);
        }
    }

    private async Task Reinstall(string name, string passPhrase, byte[] certificate)
    {
        RecoverDeletedCertificateOperation response = await certificateClient.StartRecoverDeletedCertificateAsync(name);

        await response.WaitForCompletionAsync();

        var options = new ImportCertificateOptions(name, certificate)
                      {
                          Password = passPhrase,
                          Enabled = true
                      };

        await certificateClient.ImportCertificateAsync(options);
    }

    public async Task Delete(string name)
    {
        logger.LogInformation($"Executing {nameof(Delete)} with '{{@CertificateName}}'.", name);

        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), $"Parameter '{name}' is mandatory. It can not be empty of white space.");
        }

        try
        {
            DeleteCertificateOperation response = await certificateClient.StartDeleteCertificateAsync(name);
            await response.WaitForCompletionAsync();

            logger.LogInformation($"Executed {nameof(Delete)} with '{{@CertificateName}}'.", name);
        }
        catch(RequestFailedException ex) when(ex.Message.Contains("CertificateNotFound"))
        {
            logger.LogWarning(ex, $"Executed {nameof(Delete)} with '{{@CertificateName}}': Certificate Not Found.", name);
        }
        catch(RequestFailedException ex)
        {
            logger.LogInformation($"Executing {nameof(Delete)} with '{{@CertificateName}}'.", name);
            throw new KeyVaultException($"Failed to delete certificate with name {name}", ex);
        }
    }

    private static CertificateClient CreateCertificateClient(string keyVaultName)
    {
        Uri uri = keyVaultName.ToLowerInvariant().StartsWith("https://")
                          ? new Uri(keyVaultName)
                          : new Uri($"https://{keyVaultName}.vault.azure.net");

        return new CertificateClient(uri, new AzureCredentialBuilder().Build(), new CertificateClientOptions
                                                                                {
                                                                                    Retry =
                                                                                    {
                                                                                        MaxRetries = 3,
                                                                                        Mode = RetryMode.Exponential,
                                                                                        MaxDelay = TimeSpan.FromSeconds(30)
                                                                                    }
                                                                                });
    }
}