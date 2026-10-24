using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Pricely.Common.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace Pricely.Common.Infrastructure.Keyvault;
public sealed class AzureKeyvaultClient(IConfiguration configuration)
{

    private const string KeyvaultUrl = "Keyvault:Url";
    private const string KeyvaultClientId = "Keyvault:ClientId";
    private const string KeyvaultClientCertificatePath = "Keyvault:ClientCertificatePath";
    private const string KeyvaultTenantId = "Keyvault:TenantId";


    public async Task<string> GetSecretValue(string secretName)
    {
        string keyVaultUrl = configuration.GetValueOrThrow<string>(KeyvaultUrl);
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            throw new ArgumentException("Key Vault URL is not configured.");
        }

        var credential = new ClientCertificateCredential(
            configuration.GetValueOrThrow<string>(KeyvaultTenantId),
            configuration.GetValueOrThrow<string>(KeyvaultClientId),
            configuration.GetValueOrThrow<string>(KeyvaultClientCertificatePath)
        );

        var client = new SecretClient(new Uri(keyVaultUrl), credential);

        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        return secret.Value;
    }
}
