namespace Peereflits.Shared.Cloud.KeyVault;

public class SecretNotFoundException : KeyVaultException
{
    public SecretNotFoundException(string name) : base($"A secret with name '{name}' was not found.") { }
}