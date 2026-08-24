#!/bin/bash

# Script de prueba rápida de BPT.JWE.Tool
# Este script genera llaves de prueba y demuestra el uso de la herramienta

echo "====================================="
echo "BPT JWE Tool - Demo Script"
echo "====================================="
echo ""

# Cambiar al directorio del proyecto
cd "$(dirname "$0")"

# Verificar que estamos en el directorio correcto
if [ ! -f "BPT.JWE.Tool.csproj" ]; then
    echo "❌ Error: No se encuentra BPT.JWE.Tool.csproj"
    echo "Asegúrese de ejecutar este script desde el directorio BPT.JWE.Tool"
    exit 1
fi

echo "Paso 1: Generar llaves de prueba"
echo "--------------------------------"
dotnet run -- generate-keys --output-dir ./demo-keys
echo ""

if [ ! -d "./demo-keys" ]; then
    echo "❌ Error al generar llaves"
    exit 1
fi

echo "✓ Llaves generadas en ./demo-keys"
echo ""

# Simular llaves del banco (en este demo, usamos las mismas llaves públicas)
mkdir -p ./demo-bank-keys
cp ./demo-keys/client-rsa-public.pem ./demo-bank-keys/bank-rsa-public.pem
cp ./demo-keys/client-ecdsa-public.pem ./demo-bank-keys/bank-ecdsa-public.pem
cp ./demo-keys/client-rsa-private.pem ./demo-bank-keys/bank-rsa-private.pem
cp ./demo-keys/client-ecdsa-private.pem ./demo-bank-keys/bank-ecdsa-private.pem

echo "Paso 2: Configurar la herramienta"
echo "-----------------------------------"
dotnet run -- configure \
  --client-id "DEMO_CLIENT_123" \
  --client-rsa-private "./demo-keys/client-rsa-private.pem" \
  --client-ecdsa-private "./demo-keys/client-ecdsa-private.pem" \
  --bank-rsa-public "./demo-bank-keys/bank-rsa-public.pem" \
  --bank-ecdsa-public "./demo-bank-keys/bank-ecdsa-public.pem" \
  --environment Development
echo ""

echo "Paso 3: Cifrar un request de Cash-In"
echo "--------------------------------------"
dotnet run -- encrypt \
  --input ./Examples/cashin-request.json \
  --output ./demo-cashin-encrypted.txt \
  --environment Development
echo ""

if [ ! -f "./demo-cashin-encrypted.txt" ]; then
    echo "❌ Error al cifrar"
    exit 1
fi

echo "✓ JWE cifrado guardado en: demo-cashin-encrypted.txt"
echo ""
echo "Contenido (primeros 200 caracteres):"
head -c 200 ./demo-cashin-encrypted.txt
echo "..."
echo ""

echo "Paso 4: Simular respuesta del banco"
echo "-------------------------------------"
echo "Creando una respuesta simulada del banco..."

# Crear un JSON de respuesta simulado
cat > ./demo-response.json << 'EOF'
{
  "referenceNumber": "012180001234567890",
  "amount": 1000.00,
  "externalReferenceNumber": "REF-CASHIN-001",
  "externalClientId": "CLIENT-12345",
  "status": "ACTIVE",
  "createdAt": "2025-01-28T10:30:00Z"
}
EOF

# Simular que el banco cifra la respuesta (usando las llaves del "banco")
# Para esto, necesitamos un pequeño script adicional o hacer manualmente
# Por ahora, vamos a mostrar que se puede descifrar el JWE que ciframos

echo "Paso 5: Descifrar la respuesta (demo con nuestro propio JWE)"
echo "--------------------------------------------------------------"

# Crear un JWE simulado de respuesta del banco
# Primero creamos un config temporal para simular el banco
cat > ./temp-bank-config.json << EOF
{
  "JweConfig": {
    "ClientId": "PagaTodo",
    "ClientPrivateRsaKeyPath": "./demo-bank-keys/bank-rsa-private.pem",
    "ClientPrivateEcdsaKeyPath": "./demo-bank-keys/bank-ecdsa-private.pem",
    "BankPublicRsaKeyPath": "./demo-keys/client-rsa-public.pem",
    "BankPublicEcdsaKeyPath": "./demo-keys/client-ecdsa-public.pem"
  }
}
EOF

# Crear un mini-programa C# para simular el cifrado del banco
cat > ./SimulateBankEncrypt.csx << 'CSHARPEOF'
#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.IdentityModel.JsonWebTokens, 6.31.0"
#r "nuget: Microsoft.IdentityModel.Tokens, 6.31.0"

using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var bankRsaPrivate = File.ReadAllText("./demo-bank-keys/bank-rsa-private.pem");
var bankEcdsaPrivate = File.ReadAllText("./demo-bank-keys/bank-ecdsa-private.pem");
var clientRsaPublic = File.ReadAllText("./demo-keys/client-rsa-public.pem");
var responsePayload = File.ReadAllText("./demo-response.json");

var responseEncryptionKey = RSA.Create();
var responseSigningKey = ECDsa.Create();

responseEncryptionKey.ImportFromPem(clientRsaPublic);
responseSigningKey.ImportFromPem(bankEcdsaPrivate);

var publicEncryptionKey = new RsaSecurityKey(responseEncryptionKey.ExportParameters(false));
var privateSigningKey = new ECDsaSecurityKey(responseSigningKey);

var handler = new JsonWebTokenHandler();

var token = handler.CreateToken(new SecurityTokenDescriptor()
{
    Issuer = "PagaTodo",
    Audience = "DEMO_CLIENT_123",
    IssuedAt = DateTimeOffset.Now.DateTime,
    Expires = DateTimeOffset.Now.AddHours(1).DateTime,
    Claims = new Dictionary<string, object> { { "data", responsePayload } },
    SigningCredentials = new SigningCredentials(privateSigningKey, SecurityAlgorithms.EcdsaSha256),
    EncryptingCredentials = new EncryptingCredentials(publicEncryptionKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512),
});

File.WriteAllText("./demo-bank-response.txt", token);
Console.WriteLine("✓ Respuesta del banco cifrada en: demo-bank-response.txt");
CSHARPEOF

# Intentar ejecutar el script (requiere dotnet-script)
if command -v dotnet-script &> /dev/null; then
    chmod +x ./SimulateBankEncrypt.csx
    dotnet-script ./SimulateBankEncrypt.csx
    
    if [ -f "./demo-bank-response.txt" ]; then
        echo ""
        echo "Paso 6: Descifrar la respuesta del banco"
        echo "-----------------------------------------"
        dotnet run -- decrypt \
          --input ./demo-bank-response.txt \
          --environment Development
    fi
else
    echo "ℹ️  Para simular completamente el flujo, instale dotnet-script:"
    echo "   dotnet tool install -g dotnet-script"
fi

echo ""
echo "====================================="
echo "Demo completado"
echo "====================================="
echo ""
echo "Archivos generados:"
echo "  - demo-keys/           : Llaves del cliente"
echo "  - demo-bank-keys/      : Llaves del banco (simuladas)"
echo "  - demo-cashin-encrypted.txt : Request cifrado"
echo "  - demo-response.json   : Respuesta del banco (JSON plano)"
echo ""
echo "Para limpiar:"
echo "  rm -rf demo-keys demo-bank-keys demo-*.* temp-*.* SimulateBankEncrypt.csx"
echo ""
