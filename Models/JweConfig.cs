namespace BPT_JWE_Tool.Models;

public class JweConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientPrivateRsaKeyPath { get; set; } = string.Empty;
    public string ClientPrivateEcdsaKeyPath { get; set; } = string.Empty;
    public string BankPublicRsaKeyPath { get; set; } = string.Empty;
    public string BankPublicEcdsaKeyPath { get; set; } = string.Empty;
}
