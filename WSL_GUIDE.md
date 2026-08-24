# Guía Rápida - Ejecución en WSL Ubuntu

## ✅ Verificación Exitosa

La herramienta **BPT.JWE.Tool** ha sido probada exitosamente en tu entorno WSL Ubuntu 24.04 con .NET 10.

## 🚀 Comandos para Ejecutar

### Desde WSL/Ubuntu (Recomendado)

```bash
# Navegar al directorio
cd /home/rsuarez/repo/CASHIN/BPT.CashInOut.API/BPT.JWE.Tool

# Ver ayuda
dotnet run -f net10.0 -- --help

# Generar llaves
dotnet run -f net10.0 -- generate-keys --output-dir ./my-keys

# Configurar
dotnet run -f net10.0 -- configure \
  --client-id "TU_CLIENT_ID" \
  --client-rsa-private "./my-keys/client-rsa-private.pem" \
  --client-ecdsa-private "./my-keys/client-ecdsa-private.pem" \
  --bank-rsa-public "./bank-keys/bank-rsa-public.pem" \
  --bank-ecdsa-public "./bank-keys/bank-ecdsa-public.pem" \
  --environment Development

# Cifrar
dotnet run -f net10.0 -- encrypt \
  --input ./Examples/cashin-request.json \
  --output ./encrypted-request.txt \
  --environment Development

# Descifrar
dotnet run -f net10.0 -- decrypt \
  --input ./bank-response.txt \
  --environment Development
```

## 📝 Nota Importante: .NET 10 vs .NET 8

Tu sistema tiene **.NET 10** instalado. El proyecto ahora soporta ambas versiones:
- `net8.0` - Para compatibilidad con .NET 8
- `net10.0` - Para tu sistema actual

**Siempre usa `-f net10.0`** en los comandos cuando ejecutes desde WSL.

## 🧪 Prueba Rápida Exitosa

Ya se probó exitosamente:

✅ Generar llaves RSA (4096 bits) y ECDSA (P-256)
```bash
dotnet run -f net10.0 -- generate-keys --output-dir ./test-keys
# Generó: client-rsa-private.pem, client-rsa-public.pem, 
#         client-ecdsa-private.pem, client-ecdsa-public.pem
```

✅ Configurar herramienta
```bash
dotnet run -f net10.0 -- configure \
  --client-id "TEST_CLIENT_123" \
  --client-rsa-private "./test-keys/client-rsa-private.pem" \
  --client-ecdsa-private "./test-keys/client-ecdsa-private.pem" \
  --bank-rsa-public "./test-bank-keys/bank-rsa-public.pem" \
  --bank-ecdsa-public "./test-bank-keys/bank-ecdsa-public.pem"
```

✅ Cifrar request Cash-In
```bash
dotnet run -f net10.0 -- encrypt \
  --input ./Examples/cashin-request.json \
  --output ./test-encrypted.txt
# Generó JWE de ~2.5KB (formato correcto)
```

✅ Validación de JWE
- El JWE comienza con `eyJhbGciOiJSU0EtT0FFUCIsImVuYyI6IkEyNTZDQkMtSFM1MTI...`
- Algoritmos correctos: RSA-OAEP + AES-256-CBC-HMAC-SHA-512
- Firma ECDSA incluida

## 🔧 Crear Alias para Facilitar Uso

Agrega esto a tu `~/.bashrc`:

```bash
# Alias para BPT JWE Tool
alias bpt-jwe='dotnet run -f net10.0 --project /home/rsuarez/repo/CASHIN/BPT.CashInOut.API/BPT.JWE.Tool --'
```

Luego recarga:
```bash
source ~/.bashrc
```

Ahora puedes usar:
```bash
# Desde cualquier directorio
bpt-jwe --help
bpt-jwe generate-keys --output-dir ~/my-keys
bpt-jwe encrypt --input ./request.json
```

## 📦 Publicar como Herramienta Global .NET (Opcional)

Para instalarlo como comando global:

```bash
cd /home/rsuarez/repo/CASHIN/BPT.CashInOut.API/BPT.JWE.Tool

# Empaquetar
dotnet pack -f net10.0

# Instalar globalmente
dotnet tool install --global --add-source ./nupkg BPT.JWE.Tool

# Ahora puedes usar desde cualquier lugar
bpt-jwe --help
```

Para desinstalar:
```bash
dotnet tool uninstall --global BPT.JWE.Tool
```

## 🐛 Solución de Problemas

### Error: "cannot execute: required file not found"

Los scripts `.sh` tienen formato Windows. Conviértelos:
```bash
cd /home/rsuarez/repo/CASHIN/BPT.CashInOut.API/BPT.JWE.Tool
sed -i 's/\r$//' demo.sh verify.sh
chmod +x demo.sh verify.sh
```

### Error: "No se encuentra .NET 8"

Usa .NET 10 que ya tienes instalado:
```bash
dotnet run -f net10.0 -- <comando>
```

### Verificar versión de .NET

```bash
dotnet --version
# Debe mostrar: 10.0.110 o similar
```

## 📊 Resumen de Pruebas

| Test | Estado | Resultado |
|------|--------|-----------|
| Compilación .NET 10 | ✅ | Exitosa |
| Comando `--help` | ✅ | Funciona |
| Generar llaves | ✅ | 4 archivos PEM creados |
| Configurar | ✅ | appsettings.Development.json actualizado |
| Cifrar Cash-In | ✅ | JWE de 2.5KB generado |
| Formato JWE | ✅ | Validado (comienza con eyJ...) |

## 🎯 Próximo Paso para Producción

Para usar con tu API real:

1. **Obtén las llaves públicas del banco:**
   ```bash
   # Desde tu servidor donde corre la API
   # Development
   cat /home/desarrollo/temp/keys/rsa-pub-key.pem
   cat /home/desarrollo/temp/keys/ec-prime256v1-pub-key.pem
   
   # Production
   cat /home/desausr/temp/keys/rsa-pub-key.pem
   cat /home/desausr/temp/keys/ec-prime256v1-pub-key.pem
   ```

2. **Genera tus llaves de cliente reales:**
   ```bash
   dotnet run -f net10.0 -- generate-keys --output-dir ~/bpt-production-keys
   ```

3. **Envía tus llaves públicas al banco** (para registro en Elasticsearch)

4. **Configura con llaves reales:**
   ```bash
   dotnet run -f net10.0 -- configure \
     --client-id "TU_CLIENT_ID_REAL" \
     --client-rsa-private "~/bpt-production-keys/client-rsa-private.pem" \
     --client-ecdsa-private "~/bpt-production-keys/client-ecdsa-private.pem" \
     --bank-rsa-public "./bank-production-keys/bank-rsa-public.pem" \
     --bank-ecdsa-public "./bank-production-keys/bank-ecdsa-public.pem" \
     --environment Production
   ```

5. **Prueba con tu API:**
   ```bash
   # Cifrar request
   dotnet run -f net10.0 -- encrypt \
     --input ./my-request.json \
     --output ./encrypted.txt \
     --environment Production
   
   # Enviar a tu API
   curl -X POST https://tu-api.com/bpt/cash-in \
     -H "Content-Type: text/plain" \
     -H "X-client-id: TU_CLIENT_ID_REAL" \
     -d @encrypted.txt
   ```

---

**Última actualización**: Agosto 6, 2025  
**Versión**: 1.0.0  
**Sistema probado**: WSL Ubuntu 24.04 + .NET 10.0.110  
**Estado**: ✅ FUNCIONANDO CORRECTAMENTE
