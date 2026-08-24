# 🎯 Guía de Distribución para el Equipo del Banco

## ✅ Resumen de lo Creado

Se ha desarrollado una herramienta CLI completa (`BPT.JWE.Tool`) que permite a los clientes del banco cifrar y descifrar mensajes JWE para los servicios de Cash-In y Cash-Out.

## 📦 Contenido del Proyecto

```
BPT.JWE.Tool/
├── Program.cs                    # CLI principal con 4 comandos
├── Services/JweService.cs        # Lógica de cifrado/descifrado JWE
├── Models/
│   ├── JweConfig.cs              # Modelo de configuración
│   └── ClientData.cs             # Modelo de datos del cliente
├── Examples/
│   ├── cashin-request.json       # Ejemplo real de Cash-In
│   └── cashout-request.json      # Ejemplo real de Cash-Out
├── README.md                     # Documentación completa para clientes
├── USAGE.md                      # Guía paso a paso de uso
├── SUMMARY.md                    # Resumen ejecutivo
├── INSTALL.md                    # Instrucciones de instalación
├── demo.sh                       # Script de demostración
└── BPT.JWE.Tool.csproj          # Proyecto .NET 8
```

## 🔐 Cómo Funciona

### Análisis del Código Real

La herramienta se basó en el análisis exhaustivo de tu código actual:

1. **HttpContextExtensions.cs** - Método `ValidateAndDecodeJWEToken`:
   - Descifra requests del cliente con llave privada RSA del banco
   - Verifica firma con llave pública ECDSA del cliente
   
2. **HttpContextExtensions.cs** - Método `CreateResponseJWEToken`:
   - Cifra responses con llave pública RSA del cliente
   - Firma con llave privada ECDSA del banco

3. **JweUtils.cs** - Método `GetJWE`:
   - Usado para crear JWEs en el banco
   - Mismo patrón que la herramienta CLI

### Algoritmos Implementados (100% compatibles con tu API)

```csharp
// CIFRADO (basado en tu código real)
EncryptingCredentials = new EncryptingCredentials(
    publicEncryptionKey,                    // RSA public key
    SecurityAlgorithms.RsaOAEP,            // Algoritmo de cifrado
    SecurityAlgorithms.Aes256CbcHmacSha512 // Algoritmo de contenido
)

// FIRMA (basado en tu código real)
SigningCredentials = new SigningCredentials(
    privateSigningKey,                      // ECDSA private key
    SecurityAlgorithms.EcdsaSha256         // Algoritmo de firma
)

// CLAIMS (basado en tu código real)
Claims = new Dictionary<string, object> { 
    { "data", payloadString }  // El JSON va en el claim "data"
}
```

## 🚀 Pasos para Distribuir a Clientes

### Opción 1: Distribuir como Paquete NuGet (Recomendado)

#### Paso 1: Crear el paquete

```bash
cd BPT.JWE.Tool
dotnet pack -c Release
```

Esto genera: `BPT.JWE.Tool/nupkg/BPT.JWE.Tool.1.0.0.nupkg`

#### Paso 2: Publicar en feed privado

Puedes usar:
- **Azure Artifacts** (recomendado)
- **GitHub Packages**
- **NuGet.org** (si es público)
- Servidor propio de NuGet

```bash
# Ejemplo con Azure Artifacts
dotnet nuget push ./nupkg/BPT.JWE.Tool.1.0.0.nupkg \
  --source "https://pkgs.dev.azure.com/BancoPagaTodo/_packaging/BPT-Tools/nuget/v3/index.json" \
  --api-key <tu-api-key>
```

#### Paso 3: Instruir a clientes

Envía a los clientes:

```bash
# Instalar herramienta global
dotnet tool install --global BPT.JWE.Tool \
  --add-source https://pkgs.dev.azure.com/BancoPagaTodo/_packaging/BPT-Tools/nuget/v3/index.json

# Usar
bpt-jwe --help
```

### Opción 2: Distribuir como Ejecutable Standalone

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Linux
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# macOS
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

Los ejecutables estarán en: `bin/Release/net8.0/<runtime>/publish/`

### Opción 3: Distribuir Código Fuente

Sube el proyecto a un repositorio Git (privado):

```bash
# En GitHub/Azure DevOps
git clone https://github.com/Banco-Pagatodo/BPT.JWE.Tool
cd BPT.JWE.Tool
dotnet run -- <comando>
```

## 📋 Checklist de Onboarding de Cliente

Cuando un nuevo cliente se integre:

### 1. Preparación del Banco

- [ ] Generar Client-ID único para el cliente
- [ ] Registrar en Elasticsearch (índice `clients`)
- [ ] Configurar `MustEncrypt = true` en Account

### 2. Entrega al Cliente

- [ ] Enviar ejecutable o instrucciones de instalación
- [ ] Enviar documentación (README.md, USAGE.md)
- [ ] Enviar ejemplos (Examples/cashin-request.json, cashout-request.json)
- [ ] Enviar llaves públicas del banco:
  - `bank-rsa-public.pem` (del archivo `/home/desarrollo/temp/keys/rsa-pub-key.pem` o producción)
  - `bank-ecdsa-public.pem` (del archivo `/home/desarrollo/temp/keys/ec-prime256v1-pub-key.pem` o producción)
- [ ] Enviar Client-ID asignado

### 3. Cliente Genera sus Llaves

Cliente ejecuta:
```bash
bpt-jwe generate-keys --output-dir ./my-keys
```

### 4. Cliente Envía Llaves Públicas

Cliente debe enviar:
- `client-rsa-public.pem`
- `client-ecdsa-public.pem`

### 5. Registro en Elasticsearch

Actualizar documento del cliente en Elasticsearch:

```json
{
  "id": "<client-id>",
  "accounts": [
    {
      "environment": "development",
      "rsaPublicKey": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----",
      "ecdsaPublicKey": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----",
      "mustEncrypt": true,
      "isEnabled": true
    }
  ]
}
```

### 6. Cliente Configura Herramienta

```bash
bpt-jwe configure \
  --client-id "<client-id>" \
  --client-rsa-private "./my-keys/client-rsa-private.pem" \
  --client-ecdsa-private "./my-keys/client-ecdsa-private.pem" \
  --bank-rsa-public "./bank-keys/bank-rsa-public.pem" \
  --bank-ecdsa-public "./bank-keys/bank-ecdsa-public.pem" \
  --environment Development
```

### 7. Pruebas

Cliente prueba:

```bash
# Cifrar un request de prueba
bpt-jwe encrypt --input test-request.json --output encrypted.txt

# Enviar al endpoint
curl -X POST https://api-dev.bancopagatodo.com/bpt/cash-in \
  -H "Content-Type: text/plain" \
  -H "X-client-id: <client-id>" \
  -d @encrypted.txt
```

### 8. Validación

- [ ] Request llega cifrado al banco
- [ ] Banco descifra correctamente (middleware JweRequestMiddleware)
- [ ] Banco procesa la transacción
- [ ] Banco cifra response correctamente
- [ ] Cliente descifra response correctamente

## 🔑 Gestión de Llaves del Banco

### Llaves Actuales del Banco

Según tu código, las llaves del banco están en:

**Development:**
- Llave privada RSA: `/home/desarrollo/temp/keys/rsa-priv-key.pem`
- Llave privada ECDSA: `/home/desarrollo/temp/keys/ec-prime256v1-priv-key.pem`

**Production:**
- Llave privada RSA: `/home/desausr/temp/keys/rsa-priv-key.pem`
- Llave privada ECDSA: `/home/desausr/temp/keys/ec-prime256v1-priv-key.pem`

### Generar Llaves Públicas para Enviar a Clientes

```bash
# Desde llave privada RSA, extraer pública
openssl rsa -in rsa-priv-key.pem -pubout -out bank-rsa-public.pem

# Desde llave privada ECDSA, extraer pública
openssl ec -in ec-prime256v1-priv-key.pem -pubout -out bank-ecdsa-public.pem
```

## 📧 Email Template para Clientes

```
Asunto: Herramienta de Integración JWE - Banco PagaTodo

Estimado cliente,

Le enviamos la herramienta oficial para integración con nuestros servicios de Cash-In y Cash-Out.

📦 PAQUETE INCLUIDO:
- BPT.JWE.Tool (herramienta CLI)
- Documentación completa (README.md, USAGE.md)
- Ejemplos de requests (cashin-request.json, cashout-request.json)
- Llaves públicas del banco (bank-rsa-public.pem, bank-ecdsa-public.pem)

🔑 SU CLIENT-ID: <CLIENT_ID>

📋 PASOS A SEGUIR:

1. Instale la herramienta:
   dotnet tool install --global BPT.JWE.Tool --add-source <url>

2. Genere sus llaves:
   bpt-jwe generate-keys --output-dir ./my-keys

3. Envíenos sus llaves públicas:
   - client-rsa-public.pem
   - client-ecdsa-public.pem

4. Configure la herramienta (después de que registremos sus llaves):
   bpt-jwe configure --client-id "<CLIENT_ID>" ...

5. Pruebe la integración:
   bpt-jwe encrypt --input ./Examples/cashin-request.json

📖 DOCUMENTACIÓN:
Consulte README.md para documentación completa.

🆘 SOPORTE:
soporte-integracion@bancopagatodo.com

Saludos,
Equipo de Integración
Banco PagaTodo
```

## 🧪 Testing Interno

Antes de distribuir, prueba:

```bash
cd BPT.JWE.Tool

# 1. Generar llaves de prueba
dotnet run -- generate-keys --output-dir ./test-keys

# 2. Configurar
dotnet run -- configure \
  --client-id "TEST_CLIENT" \
  --client-rsa-private "./test-keys/client-rsa-private.pem" \
  --client-ecdsa-private "./test-keys/client-ecdsa-private.pem" \
  --bank-rsa-public "<ruta-a-llave-publica-rsa-banco>" \
  --bank-ecdsa-public "<ruta-a-llave-publica-ecdsa-banco>"

# 3. Cifrar
dotnet run -- encrypt --input ./Examples/cashin-request.json --output ./test-encrypted.txt

# 4. Enviar al endpoint real de desarrollo
curl -X POST https://api-dev.bancopagatodo.com/bpt/cash-in \
  -H "Content-Type: text/plain" \
  -H "X-client-id: TEST_CLIENT" \
  -d @test-encrypted.txt \
  -o response.txt

# 5. Descifrar response
dotnet run -- decrypt --input ./response.txt
```

## 📊 Métricas a Monitorear

Después de distribuir:

- Número de clientes usando JWE
- Tasa de errores de cifrado/descifrado
- Tiempo promedio de integración
- Tickets de soporte relacionados con JWE
- Versión de herramienta utilizada por cada cliente

## 🔄 Actualizaciones Futuras

Cuando actualices la herramienta:

1. Incrementa versión en `BPT.JWE.Tool.csproj`
2. Documenta cambios en CHANGELOG.md
3. Publica nuevo paquete NuGet
4. Notifica a clientes (solo si hay breaking changes)

## 📞 Contactos Clave

- **Responsable técnico**: [Nombre]
- **Soporte nivel 1**: soporte-integracion@bancopagatodo.com
- **Soporte nivel 2**: [Email del equipo de desarrollo]
- **Repositorio**: https://github.com/Banco-Pagatodo/BPT.JWE.Tool
- **Documentación API**: [URL del portal de desarrolladores]

---

**Última actualización**: Enero 2025  
**Versión herramienta**: 1.0.0  
**Compatibilidad**: API v1.0+
