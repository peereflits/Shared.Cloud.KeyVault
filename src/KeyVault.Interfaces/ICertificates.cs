using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Peereflits.Shared.Cloud.KeyVault;

public interface ICertificates
{
    Task<X509Certificate2> Get(string name);
    Task<X509Certificate2> GetWithPrivateKey(string name, string? password = null);
    Task Install(string name, string passPhrase, byte[] certificate);
    Task Delete(string name);
}