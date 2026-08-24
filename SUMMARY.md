# 📦 Resumen de la Herramienta BPT JWE Tool

## ✅ ¿Qué se ha creado?

Se ha desarrollado una **herramienta de línea de comandos (CLI)** completa para que tus clientes puedan:

1. ✨ **Generar sus propias llaves** RSA y ECDSA
2. ⚙️ **Configurar** sus credenciales (llaves + client-id)
3. 🔐 **Cifrar requests** en formato JWE para enviar al banco
4. 🔓 **Descifrar responses** JWE recibidas del banco

## 📁 Estructura del Proyecto

```
BPT.JWE.Tool/
├── BPT.JWE.Tool.csproj      # Proyecto .NET 8
├── Program.cs               # CLI principal con comandos
├── Models/
│   ├── JweConfig.cs         # Configuración de llaves
│   └── ClientData.cs        # Datos del cliente
├── Services/
│   └── JweService.cs        # Lógica de cifrado/descifrado JWE
├── Examples/
│   ├── cashin-request.json  # Ejemplo de request Cash-In
│   └── cashout-request.json # Ejemplo de request Cash-Out
├── appsettings.json         # Configuración base
├── appsettings.Development.json
├── appsettings.Production.json
├── README.md                # Documentación completa
├── USAGE.md                 # Guía de uso paso a paso
├── INSTALL.md               # Instrucciones de instalación
├── demo.sh                  # Script de demostración
└── .gitignore               # Protección de llaves privadas
```

## 🔑 Llaves Necesarias

### Para el Cliente (lo que deben generar):

1. **Llave privada RSA** - Para descifrar responses del banco
2. **Llave pública RSA** - Para enviar al banco (el banco la usa para cifrar responses)
3. **Llave privada ECDSA** - Para firmar requests al banco
4. **Llave pública ECDSA** - Para enviar al banco (el banco la usa para verificar requests)

### Del Banco (lo que deben recibir):

1. **Llave pública RSA del banco** - Para cifrar requests
2. **Llave pública ECDSA del banco** - Para verificar responses

### Adicional:

- **Client-ID** - Identificador único asignado por el banco

## 🎯 Comandos Principales

### 1. Generar Llaves

```bash
bpt-jwe generate-keys --output-dir ./my-keys
```

Genera automáticamente:
- `client-rsa-private.pem` (PRIVADA - NO COMPARTIR)
- `client-rsa-public.pem` (PÚBLICA - ENVIAR AL BANCO)
- `client-ecdsa-private.pem` (PRIVADA - NO COMPARTIR)
- `client-ecdsa-public.pem` (PÚBLICA - ENVIAR AL BANCO)

### 2. Configurar

```bash
bpt-jwe configure \
  --client-id "MI_CLIENT_ID" \
  --client-rsa-private "./my-keys/client-rsa-private.pem" \
  --client-ecdsa-private "./my-keys/client-ecdsa-private.pem" \
  --bank-rsa-public "./bank-keys/bank-rsa-public.pem" \
  --bank-ecdsa-public "./bank-keys/bank-ecdsa-public.pem" \
  --environment Development
```

### 3. Cifrar Request

```bash
# Crear JSON del request
cat > my-request.json << EOF
{
  "amount": 1000.00,
  "isRecurring": false,
  "expirationDate": "2025-12-31T23:59:59-06:00",
  "externalReferenceNumber": "REF-001",
  "generateFromExternalReference": true,
  "externalClientId": "CLIENTE-123"
}
EOF

# Cifrar
bpt-jwe encrypt \
  --input ./my-request.json \
  --output ./request-encrypted.txt \
  --environment Development
```

### 4. Descifrar Response

```bash
# Desde archivo
bpt-jwe decrypt \
  --input ./bank-response.txt \
  --environment Development

# O directamente el JWE
bpt-jwe decrypt \
  --input "eyJhbGciOiJFUzI1NiIsImVuYyI..." \
  --environment Development
```

## 🔄 Flujo Completo

### Flujo Request (Cliente → Banco):

```
1. Cliente crea JSON del request (cashin/cashout)
   ↓
2. CLI cifra con:
   - Llave pública RSA del banco (cifrado)
   - Llave privada ECDSA del cliente (firma)
   ↓
3. Se genera JWE con estructura:
   - Issuer: ClientId
   - Audience: "PagaTodo"
   - Claims: { "data": "<json-payload>" }
   ↓
4. Cliente envía JWE al endpoint:
   POST /bpt/cash-in (o /bpt/cash-out)
   Header: X-client-id: <client-id>
   Body: <jwe-token>
```

### Flujo Response (Banco → Cliente):

```
1. Banco procesa request
   ↓
2. Banco cifra response con:
   - Llave pública RSA del cliente (cifrado)
   - Llave privada ECDSA del banco (firma)
   ↓
3. Banco envía JWE con estructura:
   - Issuer: "PagaTodo"
   - Audience: ClientId
   - Claims: { "data": "<json-response>" }
   ↓
4. CLI descifra con:
   - Llave privada RSA del cliente (descifrado)
   - Llave pública ECDSA del banco (verificación)
   ↓
5. Cliente obtiene JSON de respuesta
```

## 🔐 Detalles Técnicos del JWE

### Algoritmos Utilizados:

- **Cifrado de contenido**: RSA-OAEP (RSA con OAEP padding)
- **Cifrado de datos**: AES-256-CBC-HMAC-SHA-512
- **Firma digital**: ECDSA-SHA256 (Curva P-256)
- **Llaves RSA**: 4096 bits
- **Llaves ECDSA**: Curva nistP256 (prime256v1)

### Estructura del Token:

```json
{
  "alg": "ES256",           // ECDSA-SHA256 para firma
  "enc": "A256CBC-HS512",   // AES-256-CBC-HMAC-SHA-512 para cifrado
  "typ": "JWT"
}
{
  "iss": "ClientId" | "PagaTodo",
  "aud": "PagaTodo" | "ClientId",
  "iat": 1706437200,
  "exp": 1706440800,        // +1 hora
  "data": "<json-string>"   // Payload cifrado
}
```

## 📋 Ejemplos de Payloads

### Cash-In (Crear referencia de cobro):

```json
{
  "amount": 1000.00,
  "isRecurring": false,
  "expirationDate": "2025-12-31T23:59:59-06:00",
  "externalReferenceNumber": "REF-CASHIN-001",
  "generateFromExternalReference": true,
  "externalClientId": "CLIENT-12345",
  "person": {
    "firstName": "Juan",
    "lastName": "Pérez",
    "phoneNumber": "+525512345678",
    "email": "juan@example.com"
  }
}
```

### Cash-Out (Realizar transferencia):

```json
{
  "amount": 500.00,
  "externalClientId": "CLIENT-12345",
  "isTransfer": true,
  "externalReferenceNumber": "REF-CASHOUT-001",
  "transfer": {
    "destinationAccount": "012180001234567890",
    "destinationAccountHolder": "María González López",
    "referenceNumber": "250128",
    "concept": "Pago de servicios",
    "payerName": "Juan Pérez García",
    "payerAccount": "012180009876543210"
  }
}
```

## 🚀 Distribución a Clientes

### Opción 1: Como paquete NuGet (herramienta global)

```bash
# Empaquetar
cd BPT.JWE.Tool
dotnet pack

# El cliente instala
dotnet tool install --global --add-source <url-del-paquete> BPT.JWE.Tool

# Usar
bpt-jwe --help
```

### Opción 2: Como ejecutable standalone

```bash
# Publicar autónomo
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained

# Distribuir el ejecutable
```

### Opción 3: Como código fuente

```bash
# El cliente clona/descarga
git clone <repo-url>
cd BPT.JWE.Tool

# Ejecuta directamente
dotnet run -- <comando>
```

## 📦 Archivos para Entregar al Cliente

### Mínimo requerido:

1. ✅ `BPT.JWE.Tool/` (todo el proyecto)
2. ✅ `README.md` - Documentación completa
3. ✅ `USAGE.md` - Guía de uso paso a paso
4. ✅ `INSTALL.md` - Instrucciones de instalación
5. ✅ `Examples/` - JSONs de ejemplo
6. ✅ Llaves públicas del banco (RSA + ECDSA)

### Opcional pero recomendado:

7. ⭐ `demo.sh` - Script de demostración
8. ⭐ Documento con endpoints y especificaciones del API
9. ⭐ Video tutorial o screenshots
10. ⭐ FAQ con troubleshooting

## 🛡️ Seguridad

### ✅ Implementado:

- ✓ Cifrado RSA-OAEP con llaves de 4096 bits
- ✓ Firma digital ECDSA-SHA256
- ✓ Cifrado de contenido AES-256-CBC-HMAC-SHA512
- ✓ Validación de firma e issuer/audience
- ✓ Expiración de tokens (1 hora)
- ✓ `.gitignore` para proteger llaves privadas
- ✓ Separación de ambientes (Dev/Prod)

### ⚠️ Recomendaciones para clientes:

1. **NUNCA** compartir llaves privadas
2. **NUNCA** commitear llaves en Git
3. Rotar llaves cada 6-12 meses
4. Usar diferentes llaves para Dev y Prod
5. Almacenar llaves en gestor de secretos (Azure Key Vault, AWS Secrets Manager, etc.)
6. Limitar permisos de archivos: `chmod 600 *.pem`

## 🧪 Testing

### Probar la herramienta:

```bash
cd BPT.JWE.Tool

# Ejecutar demo
chmod +x demo.sh
./demo.sh

# O manualmente:
dotnet run -- generate-keys
dotnet run -- configure --client-id "TEST" ...
dotnet run -- encrypt --input ./Examples/cashin-request.json
```

## 📞 Soporte

Para soporte técnico:
- Email: soporte-integracion@bancopagatodo.com
- Documentación: [URL del portal de desarrolladores]
- Horario: Lunes a Viernes 9:00 - 18:00 (México)

## 📄 Licencia

© 2025 Banco PagaTodo. Todos los derechos reservados.

---

**Versión**: 1.0.0  
**Última actualización**: Enero 2025  
**Compatible con**: .NET 8.0+
