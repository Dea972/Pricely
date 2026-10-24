using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pricely.Common.Infrastructure.Configuration;

public static class ConfigurationExtensions
{

    private const string SecretPrefix = "@secret:";

    public static async Task<string> GetSecretConnectionStringOrThrowAsync(this IConfiguration configuration, string name)
    {
        string connectionStringSecretName = configuration.GetConnectionString(name);

        if (string.IsNullOrEmpty(connectionStringSecretName))
        {
            throw new ArgumentException($"Connection string {name} is not configured.");
        }

        return await configuration.GetSecretValueAsync(connectionStringSecretName) ??
               throw new InvalidOperationException($"The connection string {name} was not found");

    }

    public static string GetConnectionStringOrThrow(this IConfiguration configuration, string name)
    {
        return configuration.GetConnectionString(name) ??
               throw new InvalidOperationException($"The connection string {name} was not found");
    }

    public static T GetValueOrThrow<T>(this IConfiguration configuration, string name)
    {
        return configuration.GetValue<T?>(name) ??
               throw new InvalidOperationException($"The connection string {name} was not found");
    }


    public static async Task<string> GetSecretValueAsync(this IConfiguration configuration, string value)
    {
        if (!value.StartsWith(SecretPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // If the value does not start with the secret prefix, return it directly
            return value;
        }

        string secretName = value.Substring(SecretPrefix.Length).Trim();

        const string KeyvaultUrlPath = "Keyvault:Url";

        string keyVaultUrl = configuration.GetValueOrThrow<string>(KeyvaultUrlPath);


        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            throw new ArgumentException("Key Vault URL is not configured.");
        }


        var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        return secret.Value;
    }



    /// <summary>
    /// Récupère la valeur d'un secret en fonction du path de configuration
    /// Utilise le <paramref name="path"/> pour récupérer le secretName depuis <paramref name="configuration"/>
    /// Puie renvoie la valeur du secret en fonctione du secretName
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="path">Un chemin dans configuration</param>
    /// <returns>Une valeur secret</returns>
    /// <exception cref="InvalidOperationException">Si <paramref name="path"/> est introuvable dans <paramref name="configuration"/></exception>
    public static async Task<string> GetSecretValueFromPathAsync(this IConfiguration configuration, string path)
    {
        string secretName = configuration.GetValueOrThrow<string>(path);

        return await configuration.GetSecretValueAsync(secretName);
    }

    public static async Task ConfigureWithSecretsAsync<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath) where T : class, new()
    {
        // 1. Bind section into a fresh T instance
        var optionsInstance = new T();
        configuration.GetSection(sectionPath).Bind(optionsInstance);

        // 2. Find all public instance properties once
        PropertyInfo[] allProperties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // 3. Filter only string properties that can be secrets
        PropertyInfo[] stringProperties = allProperties
            .Where(prop => prop.CanWrite && prop.PropertyType == typeof(string))
            .ToArray();

        // 4. Resolve each secret placeholder
        foreach (PropertyInfo property in stringProperties)
        {
            string? raw = (string?)property.GetValue(optionsInstance);
            if (raw != null && raw.StartsWith(SecretPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string secretValue = await configuration.GetSecretValueAsync(raw);
                property.SetValue(optionsInstance, secretValue);
            }
        }

        // 5. Re-configure the options in DI with the fully resolved instance
        services.Configure<T>(opts =>
        {
            foreach (PropertyInfo property in allProperties)
            {
                object? value = property.GetValue(optionsInstance);
                property.SetValue(opts, value);
            }
        });
    }


}
