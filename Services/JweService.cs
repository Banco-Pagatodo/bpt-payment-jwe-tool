using System.Security.Cryptography;
using System.Text;
using BPT_JWE_Tool.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BPT_JWE_Tool.Services;

public class JweService
{
    private readonly ClientData _clientData;

    public JweService(ClientData clientData)
    {
        _clientData = clientData;
    }

    public string EncryptRequest(string jsonPayload)
    {
        if (!_clientData.IsValid())
        {
            throw new InvalidOperationException("La configuración del cliente no es válida. Asegúrese de haber configurado todas las llaves.");
        }

        var requestEncryptionKey = RSA.Create();
        var requestSigningKey = ECDsa.Create();

        requestEncryptionKey.ImportFromPem(_clientData.BankPublicRsaKey);
        requestSigningKey.ImportFromPem(_clientData.ClientPrivateEcdsaKey);

        var publicEncryptionKey = new RsaSecurityKey(requestEncryptionKey.ExportParameters(false));
        var privateSigningKey = new ECDsaSecurityKey(requestSigningKey);

        var handler = new JsonWebTokenHandler();

        var token = handler.CreateToken(new SecurityTokenDescriptor()
        {
            Issuer = _clientData.ClientId,
            Audience = "PagaTodo",
            IssuedAt = DateTimeOffset.Now.DateTime,
            Expires = DateTimeOffset.Now.AddHours(1).DateTime,
            Claims = new Dictionary<string, object> { { "data", jsonPayload } },
            SigningCredentials = new SigningCredentials(privateSigningKey, SecurityAlgorithms.EcdsaSha256),
            EncryptingCredentials = new EncryptingCredentials(publicEncryptionKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512),
        });

        return token;
    }

    public async Task<string> DecryptResponse(string jweToken)
    {
        if (!_clientData.IsValid())
        {
            throw new InvalidOperationException("La configuración del cliente no es válida. Asegúrese de haber configurado todas las llaves.");
        }

        var responseEncryptionKey = RSA.Create();
        var responseSigningKey = ECDsa.Create();

        responseEncryptionKey.ImportFromPem(_clientData.ClientPrivateRsaKey);
        responseSigningKey.ImportFromPem(_clientData.BankPublicEcdsaKey);

        var privateDecryptionKey = new RsaSecurityKey(responseEncryptionKey);
        var publicVerificationKey = new ECDsaSecurityKey(ECDsa.Create(responseSigningKey.ExportParameters(false)));

        var tokenValidationParameters = new TokenValidationParameters
        {
            RequireAudience = true,
            RequireExpirationTime = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = "PagaTodo",
            ValidAudience = _clientData.ClientId,
            TokenDecryptionKey = privateDecryptionKey,
            IssuerSigningKey = publicVerificationKey,
        };

        var handler = new JsonWebTokenHandler();

        var validationResult = await handler.ValidateTokenAsync(
            token: jweToken,
            validationParameters: tokenValidationParameters);

        if (!validationResult.IsValid)
        {
            throw new SecurityTokenValidationException($"Token inválido: {validationResult.Exception?.Message}");
        }

        if (validationResult.Claims.TryGetValue("data", out object? dataValue))
        {
            return dataValue?.ToString() ?? string.Empty;
        }

        throw new InvalidOperationException("No se encontró el claim 'data' en el token.");
    }

    public static void GenerateRsaKeyPair(string privateKeyPath, string publicKeyPath)
    {
        using var rsa = RSA.Create(4096);
        
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();

        File.WriteAllText(privateKeyPath, privateKey);
        File.WriteAllText(publicKeyPath, publicKey);

        Console.WriteLine($"Llaves RSA generadas:");
        Console.WriteLine($"  Privada: {privateKeyPath}");
        Console.WriteLine($"  Pública: {publicKeyPath}");
    }

    public static void GenerateEcdsaKeyPair(string privateKeyPath, string publicKeyPath)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        
        var privateKey = ecdsa.ExportECPrivateKeyPem();
        var publicKey = ecdsa.ExportSubjectPublicKeyInfoPem();

        File.WriteAllText(privateKeyPath, privateKey);
        File.WriteAllText(publicKeyPath, publicKey);

        Console.WriteLine($"Llaves ECDSA generadas:");
        Console.WriteLine($"  Privada: {privateKeyPath}");
        Console.WriteLine($"  Pública: {publicKeyPath}");
    }
}
