# Instalación de .NET 8 en Ubuntu 24.04 (WSL)

## Opción 1: Instalar .NET 8 SDK

```bash
# Actualizar repositorios
sudo apt-get update

# Instalar .NET 8 SDK
sudo apt-get install -y dotnet-sdk-8.0

# Verificar instalación
dotnet --list-sdks
```

## Opción 2: Usar .NET 10 (ya instalado)

Si prefieres usar .NET 10 que ya está instalado en tu sistema, puedes actualizar el proyecto:

1. Abre `BPT.JWE.Tool/BPT.JWE.Tool.csproj`
2. Cambia `<TargetFramework>net8.0</TargetFramework>` a `<TargetFramework>net10.0</TargetFramework>`
3. Recompila: `dotnet build`

## Opción 3: Multi-targeting

Para soportar ambas versiones, cambia en el .csproj:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

## Compilar la herramienta

Después de instalar .NET 8:

```bash
cd /home/rsuarez/repo/CASHIN/BPT.CashInOut.API/BPT.JWE.Tool
dotnet build
```

## Ejecutar la herramienta

```bash
# Ver ayuda
dotnet run -- --help

# Generar llaves
dotnet run -- generate-keys --output-dir ./keys

# Configurar
dotnet run -- configure \
  --client-id "TEST_CLIENT" \
  --client-rsa-private "./keys/client-rsa-private.pem" \
  --client-ecdsa-private "./keys/client-ecdsa-private.pem" \
  --bank-rsa-public "./bank-keys/bank-rsa-public.pem" \
  --bank-ecdsa-public "./bank-keys/bank-ecdsa-public.pem"

# Cifrar
dotnet run -- encrypt --input ./Examples/cashin-request.json

# Descifrar
dotnet run -- decrypt --input "eyJhbGciOi..."
```

## Publicar como herramienta global

```bash
# Empaquetar
dotnet pack

# Instalar globalmente
dotnet tool install --global --add-source ./nupkg BPT.JWE.Tool

# Usar desde cualquier lugar
bpt-jwe --help
bpt-jwe generate-keys
bpt-jwe configure --client-id "TEST"
```

## Desinstalar

```bash
dotnet tool uninstall --global BPT.JWE.Tool
```
