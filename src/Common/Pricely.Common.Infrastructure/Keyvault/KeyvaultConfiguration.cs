namespace Pricely.Common.Infrastructure.Keyvault;
public sealed class KeyvaultConfiguration
{
    public string AppId { get; set; }
    public string Url { get; set; }
    public string TenantId { get; set; }
    public string CertificatePath { get; set; }
}
