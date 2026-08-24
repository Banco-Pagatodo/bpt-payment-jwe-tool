# BPT JWE Tool

Herramienta de línea de comandos (CLI) para cifrar y descifrar mensajes JWE para los servicios de Cash-In y Cash-Out del Banco PagaTodo.

## Características

- 🔐 **Cifrado de requests**: Cifra mensajes JSON para enviarlos al banco de forma segura
- 🔓 **Descifrado de responses**: Descifra las respuestas JWE recibidas del banco
- 🔑 **Generación de llaves**: Genera pares de llaves RSA y ECDSA automáticamente
- ⚙️ **Configuración simple**: Gestión fácil de llaves y credenciales
- 🌍 **Multi-ambiente**: Soporte para Development y Production

## Instalación

### Requisitos

- .NET 8.0 SDK o superior (.NET 10 también soportado)
- Sistema operativo: Windows, Linux, o macOS

> **Nota para usuarios de WSL/Ubuntu**: Si tienes .NET 10 instalado, usa `-f net10.0` en los comandos `dotnet run`. Ver [WSL_GUIDE.md](WSL_GUIDE.md) para detalles.

### Como herramienta global .NET

```bash
dotnet pack
dotnet tool install --global --add-source ./nupkg BPT.JWE.Tool
```

### Desde el código fuente

```bash
dotnet build
dotnet run -- [comando]
```

## Configuración Inicial

### Paso 1: Generar llaves (si aún no las tiene)

```bash
bpt-jwe generate-keys --output-dir ./my-keys
```

Esto generará:
- `client-rsa-private.pem` - Llave privada RSA (GUARDAR DE FORMA SEGURA)
- `client-rsa-public.pem` - Llave pública RSA (ENVIAR AL BANCO)
- `client-ecdsa-private.pem` - Llave privada ECDSA (GUARDAR DE FORMA SEGURA)
- `client-ecdsa-public.pem` - Llave pública ECDSA (ENVIAR AL BANCO)

### Paso 2: Obtener las llaves públicas del banco

Solicite al banco:
- Llave pública RSA del banco
- Llave pública ECDSA del banco

### Paso 3: Configurar la herramienta

```bash
bpt-jwe configure \
  --client-id "YOUR_CLIENT_ID" \
  --client-rsa-private "./my-keys/client-rsa-private.pem" \
  --client-ecdsa-private "./my-keys/client-ecdsa-private.pem" \
  --bank-rsa-public "./bank-keys/bank-rsa-public.pem" \
  --bank-ecdsa-public "./bank-keys/bank-ecdsa-public.pem" \
  --environment Development
```

## Uso

### Cifrar un request para Cash-In

```bash
bpt-jwe encrypt \
  --input ./Examples/cashin-request.json \
  --output ./cashin-encrypted.txt \
  --environment Development
```

### Cifrar un request para Cash-Out

```bash
bpt-jwe encrypt \
  --input ./Examples/cashout-request.json \
  --output ./cashout-encrypted.txt \
  --environment Development
```

### Descifrar una respuesta del banco

```bash
bpt-jwe decrypt \
  --input ./bank-response.txt \
  --output ./response-decrypted.json \
  --environment Development
```

O directamente desde el JWE:

```bash
bpt-jwe decrypt \
  --input "eyJhbGciOiJFUzI1NiIsImVuYyI6IkEyNTZDQkMtSFM1MTIiLCJ0eXAiOiJKV1QifQ..." \
  --environment Development
```

## Estructura del JWE

### Request (Cliente → Banco)

El cliente cifra el payload con:
- **Algoritmo de cifrado**: RSA-OAEP (con llave pública RSA del banco)
- **Algoritmo de contenido**: AES256-CBC-HMAC-SHA512
- **Algoritmo de firma**: ECDSA-SHA256 (con llave privada ECDSA del cliente)
- **Issuer**: ClientId
- **Audience**: "PagaTodo"

### Response (Banco → Cliente)

El banco cifra el payload con:
- **Algoritmo de cifrado**: RSA-OAEP (con llave pública RSA del cliente)
- **Algoritmo de contenido**: AES256-CBC-HMAC-SHA512
- **Algoritmo de firma**: ECDSA-SHA256 (con llave privada ECDSA del banco)
- **Issuer**: "PagaTodo"
- **Audience**: ClientId

## Endpoints Soportados

- `/bpt/cash-in` - Crear referencias de cobro
- `/bpt/cash-out` - Realizar pagos o transferencias

## Seguridad

⚠️ **IMPORTANTE**:

1. **NUNCA** comparta sus llaves privadas
2. **NUNCA** haga commit de las llaves privadas en Git
3. Guarde las llaves privadas en un lugar seguro (idealmente un gestor de secretos)
4. Las llaves públicas SÍ deben compartirse con el banco
5. Use diferentes llaves para Development y Production

## Ejemplos de Payloads

Consulte la carpeta `Examples/` para ver:
- `cashin-request.json` - Ejemplo de request para Cash-In
- `cashout-request.json` - Ejemplo de request para Cash-Out

## Troubleshooting

### Error: "La configuración del cliente no es válida"

Asegúrese de haber ejecutado el comando `configure` con todos los parámetros requeridos.

### Error: "Token inválido"

- Verifique que las llaves públicas del banco sean correctas
- Verifique que su Client-ID esté correctamente configurado
- Verifique que el JWE no haya expirado (validez de 1 hora)

### Error al leer archivos PEM

Verifique que las rutas a los archivos de llaves sean correctas y que los archivos estén en formato PEM válido.

## Soporte

Para soporte técnico, contacte al equipo de integración del Banco PagaTodo.

## Licencia

© 2025 Banco PagaTodo. Todos los derechos reservados.
