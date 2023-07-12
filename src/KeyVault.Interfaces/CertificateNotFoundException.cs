using System;

namespace Peereflits.Shared.Cloud.KeyVault;

public class CertificateNotFoundException : KeyVaultException
{
    public CertificateNotFoundException() { }
    public CertificateNotFoundException(string message) : base(message) { }
    public CertificateNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}