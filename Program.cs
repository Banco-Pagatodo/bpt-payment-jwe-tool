using System.CommandLine;
using System.Text.Json;
using BPT_JWE_Tool.Models;
using BPT_JWE_Tool.Services;
using Microsoft.Extensions.Configuration;

var rootCommand = new RootCommand("Herramienta CLI para cifrar y descifrar mensajes JWE para los servicios de Cash-In y Cash-Out del Banco PagaTodo");

var configureCommand = new Command("configure", "Configurar las llaves y el client-id");
var clientIdOption = new Option<string>("--client-id", "ID del cliente") { IsRequired = true };
var clientRsaPrivateOption = new Option<string>("--client-rsa-private", "Ruta a la llave privada RSA del cliente") { IsRequired = true };
var clientEcdsaPrivateOption = new Option<string>("--client-ecdsa-private", "Ruta a la llave privada ECDSA del cliente") { IsRequired = true };
var bankRsaPublicOption = new Option<string>("--bank-rsa-public", "Ruta a la llave pública RSA del banco") { IsRequired = true };
var bankEcdsaPublicOption = new Option<string>("--bank-ecdsa-public", "Ruta a la llave pública ECDSA del banco") { IsRequired = true };
var environmentOption = new Option<string>("--environment", () => "Development", "Ambiente (Development o Production)");

configureCommand.AddOption(clientIdOption);
configureCommand.AddOption(clientRsaPrivateOption);
configureCommand.AddOption(clientEcdsaPrivateOption);
configureCommand.AddOption(bankRsaPublicOption);
configureCommand.AddOption(bankEcdsaPublicOption);
configureCommand.AddOption(environmentOption);

configureCommand.SetHandler((clientId, clientRsaPrivate, clientEcdsaPrivate, bankRsaPublic, bankEcdsaPublic, environment) =>
{
    var settingsFile = environment == "Production" ? "appsettings.Production.json" : "appsettings.Development.json";
    
    var config = new JweConfig
    {
        ClientId = clientId,
        ClientPrivateRsaKeyPath = clientRsaPrivate,
        ClientPrivateEcdsaKeyPath = clientEcdsaPrivate,
        BankPublicRsaKeyPath = bankRsaPublic,
        BankPublicEcdsaKeyPath = bankEcdsaPublic
    };

    var json = JsonSerializer.Serialize(new { JweConfig = config }, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(settingsFile, json);

    Console.WriteLine($"✓ Configuración guardada en {settingsFile}");
    Console.WriteLine($"  Client ID: {clientId}");
    Console.WriteLine($"  Llave privada RSA: {clientRsaPrivate}");
    Console.WriteLine($"  Llave privada ECDSA: {clientEcdsaPrivate}");
    Console.WriteLine($"  Llave pública RSA del banco: {bankRsaPublic}");
    Console.WriteLine($"  Llave pública ECDSA del banco: {bankEcdsaPublic}");
}, clientIdOption, clientRsaPrivateOption, clientEcdsaPrivateOption, bankRsaPublicOption, bankEcdsaPublicOption, environmentOption);

var encryptCommand = new Command("encrypt", "Cifrar un mensaje JSON para enviarlo al banco");
var inputFileOption = new Option<string>("--input", "Ruta al archivo JSON con el payload") { IsRequired = true };
var outputFileOption = new Option<string>("--output", "Ruta donde guardar el JWE cifrado (opcional)");
var envOption = new Option<string>("--environment", () => "Development", "Ambiente (Development o Production)");

encryptCommand.AddOption(inputFileOption);
encryptCommand.AddOption(outputFileOption);
encryptCommand.AddOption(envOption);

encryptCommand.SetHandler((inputFile, outputFile, env) =>
{
    try
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: false)
            .Build();

        var config = configuration.GetSection("JweConfig").Get<JweConfig>();
        
        if (config == null)
        {
            Console.WriteLine("❌ Error: No se encontró la configuración. Ejecute primero 'configure'.");
            return;
        }

        var clientData = new ClientData
        {
            ClientId = config.ClientId,
            ClientPrivateRsaKey = File.ReadAllText(config.ClientPrivateRsaKeyPath),
            ClientPrivateEcdsaKey = File.ReadAllText(config.ClientPrivateEcdsaKeyPath),
            BankPublicRsaKey = File.ReadAllText(config.BankPublicRsaKeyPath),
            BankPublicEcdsaKey = File.ReadAllText(config.BankPublicEcdsaKeyPath)
        };

        if (!clientData.IsValid())
        {
            Console.WriteLine("❌ Error: La configuración no es válida. Verifique que todas las llaves estén configuradas.");
            return;
        }

        var jsonPayload = File.ReadAllText(inputFile);
        
        var jweService = new JweService(clientData);
        var encryptedToken = jweService.EncryptRequest(jsonPayload);

        if (!string.IsNullOrEmpty(outputFile))
        {
            File.WriteAllText(outputFile, encryptedToken);
            Console.WriteLine($"✓ JWE cifrado guardado en: {outputFile}");
        }
        else
        {
            Console.WriteLine("✓ JWE cifrado:");
            Console.WriteLine(encryptedToken);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al cifrar: {ex.Message}");
    }
}, inputFileOption, outputFileOption, envOption);

var decryptCommand = new Command("decrypt", "Descifrar un mensaje JWE recibido del banco");
var jweInputOption = new Option<string>("--input", "Ruta al archivo con el JWE o el JWE directamente") { IsRequired = true };
var decryptOutputOption = new Option<string>("--output", "Ruta donde guardar el JSON descifrado (opcional)");
var decryptEnvOption = new Option<string>("--environment", () => "Development", "Ambiente (Development o Production)");

decryptCommand.AddOption(jweInputOption);
decryptCommand.AddOption(decryptOutputOption);
decryptCommand.AddOption(decryptEnvOption);

decryptCommand.SetHandler(async (jweInput, outputFile, env) =>
{
    try
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: false)
            .Build();

        var config = configuration.GetSection("JweConfig").Get<JweConfig>();
        
        if (config == null)
        {
            Console.WriteLine("❌ Error: No se encontró la configuración. Ejecute primero 'configure'.");
            return;
        }

        var clientData = new ClientData
        {
            ClientId = config.ClientId,
            ClientPrivateRsaKey = File.ReadAllText(config.ClientPrivateRsaKeyPath),
            ClientPrivateEcdsaKey = File.ReadAllText(config.ClientPrivateEcdsaKeyPath),
            BankPublicRsaKey = File.ReadAllText(config.BankPublicRsaKeyPath),
            BankPublicEcdsaKey = File.ReadAllText(config.BankPublicEcdsaKeyPath)
        };

        if (!clientData.IsValid())
        {
            Console.WriteLine("❌ Error: La configuración no es válida. Verifique que todas las llaves estén configuradas.");
            return;
        }

        var jweToken = File.Exists(jweInput) ? File.ReadAllText(jweInput).Trim() : jweInput.Trim();

        var jweService = new JweService(clientData);
        var decryptedPayload = await jweService.DecryptResponse(jweToken);

        if (!string.IsNullOrEmpty(outputFile))
        {
            File.WriteAllText(outputFile, decryptedPayload);
            Console.WriteLine($"✓ JSON descifrado guardado en: {outputFile}");
        }
        else
        {
            Console.WriteLine("✓ JSON descifrado:");
            var formattedJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(decryptedPayload), new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(formattedJson);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al descifrar: {ex.Message}");
    }
}, jweInputOption, decryptOutputOption, decryptEnvOption);

var generateKeysCommand = new Command("generate-keys", "Generar un par de llaves RSA y ECDSA");
var outputDirOption = new Option<string>("--output-dir", () => "./keys", "Directorio donde guardar las llaves");

generateKeysCommand.AddOption(outputDirOption);

generateKeysCommand.SetHandler((outputDir) =>
{
    try
    {
        Directory.CreateDirectory(outputDir);
        
        var rsaPrivatePath = Path.Combine(outputDir, "client-rsa-private.pem");
        var rsaPublicPath = Path.Combine(outputDir, "client-rsa-public.pem");
        var ecdsaPrivatePath = Path.Combine(outputDir, "client-ecdsa-private.pem");
        var ecdsaPublicPath = Path.Combine(outputDir, "client-ecdsa-public.pem");

        JweService.GenerateRsaKeyPair(rsaPrivatePath, rsaPublicPath);
        JweService.GenerateEcdsaKeyPair(ecdsaPrivatePath, ecdsaPublicPath);

        Console.WriteLine();
        Console.WriteLine("✓ Llaves generadas exitosamente en: " + outputDir);
        Console.WriteLine();
        Console.WriteLine("IMPORTANTE:");
        Console.WriteLine("  1. Guarde las llaves privadas en un lugar seguro");
        Console.WriteLine("  2. Envíe las llaves públicas al banco para su registro");
        Console.WriteLine($"  3. Llaves públicas a enviar:");
        Console.WriteLine($"     - RSA: {rsaPublicPath}");
        Console.WriteLine($"     - ECDSA: {ecdsaPublicPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al generar llaves: {ex.Message}");
    }
}, outputDirOption);

rootCommand.AddCommand(configureCommand);
rootCommand.AddCommand(encryptCommand);
rootCommand.AddCommand(decryptCommand);
rootCommand.AddCommand(generateKeysCommand);

return await rootCommand.InvokeAsync(args);
