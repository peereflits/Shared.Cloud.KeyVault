using System.Threading.Tasks;

namespace Peereflits.Shared.Cloud.KeyVault;

public interface ISecrets
{
    string Get(string name);
    Task<string> GetAsync(string name);
}