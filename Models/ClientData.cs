namespace BPT_JWE_Tool.Models;

public class ClientData
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientPrivateRsaKey { get; set; } = string.Empty;
    public string ClientPrivateEcdsaKey { get; set; } = string.Empty;
    public string BankPublicRsaKey { get; set; } = string.Empty;
    public string BankPublicEcdsaKey { get; set; } = string.Empty;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ClientId) &&
               !string.IsNullOrEmpty(ClientPrivateRsaKey) &&
               !string.IsNullOrEmpty(ClientPrivateEcdsaKey) &&
               !string.IsNullOrEmpty(BankPublicRsaKey) &&
               !string.IsNullOrEmpty(BankPublicEcdsaKey);
    }
}
