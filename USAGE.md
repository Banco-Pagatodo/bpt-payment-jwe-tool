# Guía de Uso - BPT JWE Tool

## Flujo completo de uso

### 1. Primera vez - Configuración inicial

#### 1.1 Generar sus llaves

```bash
# Generar llaves en el directorio ./keys
dotnet run -- generate-keys --output-dir ./keys
```

Esto creará:
```
keys/
├── client-rsa-private.pem      # SU llave privada RSA - NO COMPARTIR
├── client-rsa-public.pem       # SU llave pública RSA - ENVIAR AL BANCO
├── client-ecdsa-private.pem    # SU llave privada ECDSA - NO COMPARTIR
└── client-ecdsa-public.pem     # SU llave pública ECDSA - ENVIAR AL BANCO
```

#### 1.2 Enviar llaves públicas al banco

Envíe al banco los siguientes archivos:
- `client-rsa-public.pem`
- `client-ecdsa-public.pem`

Junto con su **Client-ID** asignado.

#### 1.3 Recibir llaves públicas del banco

El banco le proporcionará:
- `bank-rsa-public.pem` - Llave pública RSA del banco
- `bank-ecdsa-public.pem` - Llave pública ECDSA del banco

Guárdelos en un directorio, por ejemplo `./bank-keys/`

#### 1.4 Configurar la herramienta

```bash
dotnet run -- configure \
  --client-id "MI_CLIENT_ID_ASIGNADO" \
  --client-rsa-private "./keys/client-rsa-private.pem" \
  --client-ecdsa-private "./keys/client-ecdsa-private.pem" \
  --bank-rsa-public "./bank-keys/bank-rsa-public.pem" \
  --bank-ecdsa-public "./bank-keys/bank-ecdsa-public.pem" \
  --environment Development
```

### 2. Uso diario - Cifrar requests

#### 2.1 Preparar el JSON del request

Cree un archivo JSON con los datos que desea enviar. Ejemplo para Cash-In:

```json
{
  "amount": 1000.00,
  "isRecurring": false,
  "expirationDate": "2025-12-31T23:59:59-06:00",
  "externalReferenceNumber": "REF-001",
  "generateFromExternalReference": true,
  "externalClientId": "CLIENTE-123",
  "person": {
    "firstName": "Juan",
    "lastName": "Pérez",
    "phoneNumber": "+525512345678",
    "email": "juan@example.com"
  }
}
```

Guárdelo como `my-request.json`

#### 2.2 Cifrar el request

```bash
dotnet run -- encrypt \
  --input ./my-request.json \
  --output ./request-encrypted.txt \
  --environment Development
```

#### 2.3 Enviar al banco

Lea el contenido de `request-encrypted.txt` y envíelo al endpoint correspondiente:

```bash
# Ejemplo con curl
curl -X POST https://api.banco.com/bpt/cash-in \
  -H "Content-Type: text/plain" \
  -H "X-client-id: MI_CLIENT_ID_ASIGNADO" \
  -d @request-encrypted.txt
```

### 3. Uso diario - Descifrar responses

#### 3.1 Guardar la respuesta del banco

Guarde el JWE que recibió del banco en un archivo, por ejemplo `bank-response.txt`

#### 3.2 Descifrar la respuesta

```bash
dotnet run -- decrypt \
  --input ./bank-response.txt \
  --output ./response-decrypted.json \
  --environment Development
```

#### 3.3 Ver el resultado

```bash
cat response-decrypted.json
```

O descifrar directamente a consola:

```bash
dotnet run -- decrypt \
  --input ./bank-response.txt \
  --environment Development
```

## Ejemplos prácticos

### Ejemplo 1: Crear una referencia de Cash-In

```bash
# 1. Crear el JSON
cat > cashin.json << EOF
{
  "amount": 500.00,
  "isRecurring": false,
  "expirationDate": "2025-06-30T23:59:59-06:00",
  "externalReferenceNumber": "VENTA-12345",
  "generateFromExternalReference": true,
  "externalClientId": "TIENDA-ONLINE",
  "person": {
    "firstName": "María",
    "lastName": "González",
    "phoneNumber": "+525587654321",
    "email": "maria@example.com"
  }
}
EOF

# 2. Cifrar
dotnet run -- encrypt --input ./cashin.json --output ./cashin-encrypted.txt

# 3. Enviar (ejemplo con curl)
curl -X POST https://api-dev.banco.com/bpt/cash-in \
  -H "Content-Type: text/plain" \
  -H "X-client-id: TIENDA-ONLINE" \
  -d @cashin-encrypted.txt \
  -o response.txt

# 4. Descifrar respuesta
dotnet run -- decrypt --input ./response.txt
```

### Ejemplo 2: Realizar un Cash-Out (transferencia)

```bash
# 1. Crear el JSON
cat > cashout.json << EOF
{
  "amount": 1500.00,
  "externalClientId": "TIENDA-ONLINE",
  "isTransfer": true,
  "externalReferenceNumber": "PAGO-67890",
  "transfer": {
    "destinationAccount": "012180001234567890",
    "destinationAccountHolder": "Carlos Ramírez",
    "referenceNumber": "250128",
    "concept": "Pago a proveedor",
    "payerName": "Mi Empresa S.A.",
    "payerAccount": "012180009876543210"
  }
}
EOF

# 2. Cifrar
dotnet run -- encrypt --input ./cashout.json --output ./cashout-encrypted.txt

# 3. Enviar
curl -X POST https://api-dev.banco.com/bpt/cash-out \
  -H "Content-Type: text/plain" \
  -H "X-client-id: TIENDA-ONLINE" \
  -d @cashout-encrypted.txt \
  -o response.txt

# 4. Descifrar respuesta
dotnet run -- decrypt --input ./response.txt
```

### Ejemplo 3: Configuración para Production

```bash
# 1. Configurar para producción (con llaves diferentes)
dotnet run -- configure \
  --client-id "PROD_CLIENT_ID" \
  --client-rsa-private "./keys/prod/client-rsa-private.pem" \
  --client-ecdsa-private "./keys/prod/client-ecdsa-private.pem" \
  --bank-rsa-public "./bank-keys/prod/bank-rsa-public.pem" \
  --bank-ecdsa-public "./bank-keys/prod/bank-ecdsa-public.pem" \
  --environment Production

# 2. Usar en producción
dotnet run -- encrypt \
  --input ./request.json \
  --output ./encrypted.txt \
  --environment Production
```

## Scripts de automatización

### Script Bash para cifrar y enviar

```bash
#!/bin/bash
# encrypt-and-send.sh

REQUEST_FILE=$1
ENDPOINT=$2
CLIENT_ID=$3
ENVIRONMENT=${4:-Development}

# Cifrar
dotnet run --project ./BPT.JWE.Tool -- encrypt \
  --input "$REQUEST_FILE" \
  --output ./temp-encrypted.txt \
  --environment "$ENVIRONMENT"

# Enviar
curl -X POST "$ENDPOINT" \
  -H "Content-Type: text/plain" \
  -H "X-client-id: $CLIENT_ID" \
  -d @temp-encrypted.txt \
  -o temp-response.txt

# Descifrar respuesta
dotnet run --project ./BPT.JWE.Tool -- decrypt \
  --input ./temp-response.txt \
  --environment "$ENVIRONMENT"

# Limpiar
rm temp-encrypted.txt temp-response.txt
```

Uso:
```bash
./encrypt-and-send.sh cashin.json https://api-dev.banco.com/bpt/cash-in TIENDA-ONLINE Development
```

## Comandos útiles

```bash
# Ver ayuda general
dotnet run -- --help

# Ver ayuda de un comando específico
dotnet run -- encrypt --help
dotnet run -- decrypt --help
dotnet run -- configure --help
dotnet run -- generate-keys --help

# Verificar configuración actual
cat appsettings.Development.json

# Probar con los ejemplos incluidos
dotnet run -- encrypt --input ./Examples/cashin-request.json
```

## Tips y mejores prácticas

1. **Use variables de entorno para producción**:
   ```bash
   export BPT_CLIENT_ID="PROD_CLIENT_ID"
   export BPT_RSA_PRIVATE="/secure/path/rsa-private.pem"
   ```

2. **Valide los JSONs antes de cifrar**:
   ```bash
   cat request.json | jq .  # Valida que sea JSON válido
   ```

3. **Guarde logs de transacciones**:
   ```bash
   dotnet run -- encrypt --input req.json 2>&1 | tee encrypt.log
   ```

4. **Rote sus llaves periódicamente**:
   - Genere nuevas llaves cada 6-12 meses
   - Coordine con el banco el cambio de llaves

5. **Use diferentes llaves para cada ambiente**:
   - Development: llaves de prueba
   - Production: llaves de producción seguras

## Troubleshooting

### No se encuentra la configuración

```bash
# Verificar que existe el archivo
ls -la appsettings.Development.json

# Recrear configuración
dotnet run -- configure [opciones]
```

### Error al leer archivos PEM

```bash
# Verificar formato del archivo
head -1 keys/client-rsa-private.pem
# Debe mostrar: -----BEGIN RSA PRIVATE KEY----- o similar

# Verificar permisos
ls -l keys/
```

### Token expirado

Los tokens JWE tienen validez de 1 hora. Si recibe un error de token expirado, simplemente genere un nuevo JWE cifrado.

## Soporte

Para preguntas o problemas, contacte a: soporte-integracion@bancopagatodo.com
