namespace Peereflits.Shared.Cloud.KeyVault;

public interface IFactory
{
    ISecrets CreateSecrets();
    ICertificates CreateCertificates();
}