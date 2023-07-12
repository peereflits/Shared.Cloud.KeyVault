using System;

namespace Peereflits.Shared.Cloud.KeyVault;

public class KeyVaultException : Exception
{
    public KeyVaultException() { }

    public KeyVaultException(string message) : base(message) { }

    public KeyVaultException(string message, Exception innerException) : base(message, innerException) { }
}