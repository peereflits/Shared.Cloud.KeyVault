using System;
using Azure.Core;
using Azure.Identity;

namespace Peereflits.Shared.Cloud.KeyVault;

internal class AzureCredentialBuilder
{
    public TokenCredential Build()
    {
        var option = new DefaultAzureCredentialOptions
                     {
                         ExcludeEnvironmentCredential = !IsEnvironment,
                         ExcludeManagedIdentityCredential = !IsManagedIdentity,
                         ExcludeVisualStudioCredential = !IsDevelopment,
                         ExcludeAzureCliCredential = true,
                         ExcludeAzurePowerShellCredential = true,
                         ExcludeInteractiveBrowserCredential = true,
                         ExcludeSharedTokenCacheCredential = true,
                         ExcludeVisualStudioCodeCredential = true
                     };

        string? tenant = Environment.GetEnvironmentVariable("AZURE_TENANT_ID")
                      ?? Environment.GetEnvironmentVariable("TENANT_ID");

        if(!string.IsNullOrEmpty(tenant))
        {
            option.TenantId = tenant;
            option.VisualStudioTenantId = tenant;
        }

        return new DefaultAzureCredential(option);
    }

    private static bool IsManagedIdentity => (HasEnvVar("IDENTITY_ENDPOINT") && HasEnvVar("IDENTITY_HEADER")) 
                                          || (HasEnvVar("MSI_ENDPOINT") && HasEnvVar("MSI_SECRET"));

    private static bool IsEnvironment => HasEnvVar("AZURE_TENANT_ID")
                                      && HasEnvVar("AZURE_CLIENT_ID")
                                      && HasEnvVar("AZURE_CLIENT_SECRET");

    private static readonly bool IsDevelopment = !(IsManagedIdentity  || IsEnvironment);

    private static bool HasEnvVar(string name) => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name));
}