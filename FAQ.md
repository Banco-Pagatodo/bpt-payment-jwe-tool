# ❓ FAQ y Troubleshooting

## Preguntas Frecuentes

### General

**P: ¿Qué es JWE?**  
R: JSON Web Encryption (JWE) es un estándar para cifrar datos de forma segura. Combina cifrado asimétrico (RSA) con cifrado simétrico (AES) para proteger la información.

**P: ¿Por qué necesito 4 llaves (2 RSA + 2 ECDSA)?**  
R: 
- **RSA**: Para cifrado/descifrado de datos
  - Tu llave privada RSA: Para descifrar responses del banco
  - Llave pública RSA del banco: Para cifrar requests al banco
- **ECDSA**: Para firma/verificación digital
  - Tu llave privada ECDSA: Para firmar requests (demostrar que eres tú)
  - Llave pública ECDSA del banco: Para verificar responses (asegurar que vienen del banco)

**P: ¿Cuánto tiempo son válidos los tokens JWE?**  
R: Los tokens JWE tienen una validez de **1 hora** desde su creación. Después de ese tiempo, el banco los rechazará y deberás generar un nuevo JWE.

**P: ¿Puedo usar las mismas llaves en Development y Production?**  
R: **NO**. Por seguridad, debes usar llaves diferentes para cada ambiente. Genera un set de llaves para Development y otro para Production.

**P: ¿Debo commitear las llaves en Git?**  
R: **NUNCA** hagas commit de las llaves privadas. El archivo `.gitignore` ya está configurado para protegerlas, pero siempre verifica antes de hacer push.

**P: ¿Qué hago si pierdo mis llaves privadas?**  
R: 
1. Genera un nuevo par de llaves con `bpt-jwe generate-keys`
2. Envía las nuevas llaves públicas al banco
3. Espera confirmación del banco
4. Reconfigura la herramienta con las nuevas llaves

**P: ¿Puedo usar esta herramienta en un servidor/automatización?**  
R: Sí, la herramienta está diseñada para usarse tanto manualmente como en scripts automatizados (CI/CD, cron jobs, etc.).

**P: ¿Qué información va dentro del JWE?**  
R: El JWE contiene tu request completo en formato JSON (ejemplo: datos de una transacción Cash-In o Cash-Out) en el claim llamado `"data"`.

### Instalación y Configuración

**P: ¿Qué versión de .NET necesito?**  
R: .NET 8.0 SDK o superior. Verifica con: `dotnet --version`

**P: ¿Cómo instalo .NET 8?**  
R: Consulta el archivo `INSTALL.md` incluido en el paquete, o visita: https://dotnet.microsoft.com/download

**P: La herramienta no se encuentra después de instalarla**  
R: 
1. Verifica que las herramientas globales .NET estén en tu PATH
2. En Windows: `%USERPROFILE%\.dotnet\tools`
3. En Linux/Mac: `~/.dotnet/tools`
4. Reinicia tu terminal después de instalar

**P: ¿Cómo actualizo la herramienta a una nueva versión?**  
R:
```bash
dotnet tool update --global BPT.JWE.Tool
```

**P: Error: "No se encontró la configuración"**  
R: Ejecuta primero el comando `configure`:
```bash
bpt-jwe configure \
  --client-id "TU_CLIENT_ID" \
  --client-rsa-private "./ruta/client-rsa-private.pem" \
  --client-ecdsa-private "./ruta/client-ecdsa-private.pem" \
  --bank-rsa-public "./ruta/bank-rsa-public.pem" \
  --bank-ecdsa-public "./ruta/bank-ecdsa-public.pem" \
  --environment Development
```

### Generación de Llaves

**P: ¿Las llaves generadas son seguras?**  
R: Sí, la herramienta usa las bibliotecas criptográficas estándar de .NET:
- RSA 4096 bits (muy seguro)
- ECDSA con curva nistP256 (estándar recomendado)

**P: ¿Puedo generar llaves con OpenSSL en lugar de la herramienta?**  
R: Sí, pero asegúrate de usar los mismos algoritmos:
```bash
# RSA 4096
openssl genrsa -out client-rsa-private.pem 4096
openssl rsa -in client-rsa-private.pem -pubout -out client-rsa-public.pem

# ECDSA P-256
openssl ecparam -name prime256v1 -genkey -out client-ecdsa-private.pem
openssl ec -in client-ecdsa-private.pem -pubout -out client-ecdsa-public.pem
```

**P: ¿En qué formato deben estar las llaves?**  
R: En formato **PEM** (Privacy Enhanced Mail). Los archivos deben comenzar con:
- RSA Private: `-----BEGIN RSA PRIVATE KEY-----`
- RSA Public: `-----BEGIN PUBLIC KEY-----`
- ECDSA Private: `-----BEGIN EC PRIVATE KEY-----` o `-----BEGIN PRIVATE KEY-----`
- ECDSA Public: `-----BEGIN PUBLIC KEY-----`

### Cifrado

**P: Error: "La configuración del cliente no es válida"**  
R: Verifica que:
1. Hayas ejecutado `configure` correctamente
2. Todos los archivos de llaves existan en las rutas especificadas
3. El Client-ID esté configurado

**P: ¿Puedo cifrar cualquier JSON?**  
R: Puedes cifrar cualquier JSON válido, pero el banco solo procesará requests que cumplan con el schema esperado (ver `Examples/cashin-request.json` y `Examples/cashout-request.json`).

**P: El JWE generado es muy largo, ¿es normal?**  
R: Sí, los JWE son largos porque contienen:
- Header cifrado
- Llave de contenido cifrada (con RSA)
- Payload cifrado (con AES)
- Firma digital (con ECDSA)
- Metadata en Base64

Un JWE típico tiene entre 1,500 y 3,000 caracteres.

**P: ¿Puedo ver el contenido del JWE sin descifrarlo?**  
R: No directamente, pero puedes usar un debugger JWT como https://jwt.io (ADVERTENCIA: nunca pegues JWEs con datos reales en servicios públicos).

### Descifrado

**P: Error: "Token inválido"**  
R: Posibles causas:
1. **Token expirado**: Los JWE expiran en 1 hora
2. **Llaves incorrectas**: Verifica que uses las llaves públicas correctas del banco
3. **Client-ID incorrecto**: Verifica que el Client-ID configurado coincida con el registrado en el banco
4. **Formato corrupto**: Verifica que el JWE esté completo (no cortado)

**P: Error: "No se encontró el claim 'data' en el token"**  
R: El JWE no tiene la estructura esperada. Verifica que provenga del banco y no esté corrupto.

**P: El JSON descifrado tiene caracteres extraños**  
R: Problema de encoding. El banco usa UTF-8. Verifica que tu terminal soporte UTF-8.

### Envío de Requests

**P: ¿Cómo envío el JWE al banco?**  
R: Usa HTTP POST con:
```bash
curl -X POST https://api.bancopagatodo.com/bpt/cash-in \
  -H "Content-Type: text/plain" \
  -H "X-client-id: TU_CLIENT_ID" \
  -d @encrypted-request.txt
```

**P: ¿Qué endpoint uso para Cash-In vs Cash-Out?**  
R:
- Cash-In (crear referencia): `POST /bpt/cash-in`
- Cash-Out (transferencia/retiro): `POST /bpt/cash-out`

**P: Error 400: "Invalid JWE Token"**  
R: El banco no pudo descifrar/validar tu JWE. Verifica:
1. El Client-ID en el header coincide con el configurado
2. El banco tiene tus llaves públicas registradas
3. No pasó más de 1 hora desde que generaste el JWE
4. El JWE está completo (no cortado)

**P: Error 401: Unauthorized**  
R: 
1. Verifica el Client-ID
2. Verifica que tu cuenta esté activa en el banco
3. Verifica que `mustEncrypt` esté configurado correctamente

**P: Error 500 del servidor**  
R: Error interno del banco. Contacta a soporte con:
- Timestamp del request
- Client-ID
- Endpoint usado
- Mensaje de error completo

## Troubleshooting por Síntoma

### "No funciona nada"

1. ¿Instalaste .NET 8?
   ```bash
   dotnet --version
   # Debe mostrar 8.x.x o superior
   ```

2. ¿Compilaste el proyecto?
   ```bash
   cd BPT.JWE.Tool
   dotnet build
   ```

3. ¿Puedes ejecutar la ayuda?
   ```bash
   dotnet run -- --help
   ```

### "generate-keys no genera archivos"

1. ¿Tienes permisos de escritura en el directorio?
   ```bash
   # Linux/Mac
   ls -la
   
   # Windows
   icacls .
   ```

2. ¿El directorio existe?
   ```bash
   mkdir -p ./keys
   dotnet run -- generate-keys --output-dir ./keys
   ```

### "configure no guarda la configuración"

1. Verifica que los archivos de llaves existan:
   ```bash
   ls -la ./ruta/client-rsa-private.pem
   ```

2. Verifica permisos de escritura en el directorio del proyecto

3. Mira el archivo generado:
   ```bash
   cat appsettings.Development.json
   ```

### "encrypt falla"

1. ¿Configuraste la herramienta?
   ```bash
   cat appsettings.Development.json
   # Debe tener todos los campos llenos
   ```

2. ¿El JSON de input es válido?
   ```bash
   cat tu-request.json | jq .
   # Debe mostrar el JSON formateado sin errores
   ```

3. ¿Los archivos de llaves existen y son accesibles?
   ```bash
   cat ./keys/client-ecdsa-private.pem | head -1
   # Debe mostrar: -----BEGIN EC PRIVATE KEY----- o similar
   ```

### "decrypt falla"

1. ¿El JWE está completo?
   ```bash
   wc -c encrypted.txt
   # Debe tener al menos 1000 caracteres
   ```

2. ¿Tienes las llaves públicas correctas del banco?
   - Solicita al banco verificar que enviaron las llaves correctas

3. ¿El Client-ID coincide?
   ```bash
   cat appsettings.Development.json | jq .JweConfig.ClientId
   # Debe coincidir con el registrado en el banco
   ```

### "El banco rechaza mi request"

1. ¿Enviaste tus llaves públicas al banco?
   - Verifica que el banco las haya registrado

2. ¿El header X-client-id es correcto?
   ```bash
   curl -v ...
   # Verifica en la salida que el header se envíe
   ```

3. ¿El ambiente es correcto (Development vs Production)?
   - Usa `--environment Development` para pruebas
   - Usa `--environment Production` para producción

## Comandos de Diagnóstico

### Verificar configuración actual

```bash
# Development
cat appsettings.Development.json | jq .

# Production
cat appsettings.Production.json | jq .
```

### Verificar llaves

```bash
# Ver header de llave privada RSA
head -1 ./keys/client-rsa-private.pem

# Ver header de llave pública RSA
head -1 ./keys/client-rsa-public.pem

# Verificar que las llaves coincidan (RSA)
openssl rsa -in ./keys/client-rsa-private.pem -pubout | diff - ./keys/client-rsa-public.pem

# Verificar que las llaves coincidan (ECDSA)
openssl ec -in ./keys/client-ecdsa-private.pem -pubout | diff - ./keys/client-ecdsa-public.pem
```

### Validar JSON

```bash
# Con jq
cat request.json | jq .

# Con Python
python -m json.tool request.json

# Con Node
node -e "JSON.parse(require('fs').readFileSync('request.json'))"
```

### Test completo end-to-end

```bash
# 1. Generar llaves
dotnet run -- generate-keys --output-dir ./test

# 2. Configurar (simulando que bank keys = client keys para test)
dotnet run -- configure \
  --client-id "TEST" \
  --client-rsa-private "./test/client-rsa-private.pem" \
  --client-ecdsa-private "./test/client-ecdsa-private.pem" \
  --bank-rsa-public "./test/client-rsa-public.pem" \
  --bank-ecdsa-public "./test/client-ecdsa-public.pem"

# 3. Cifrar ejemplo
dotnet run -- encrypt \
  --input ./Examples/cashin-request.json \
  --output ./test.txt

# 4. Verificar que se generó
ls -lh test.txt

# 5. Limpiar
rm -rf test test.txt
```

## Contacto de Soporte

Si después de consultar este FAQ aún tienes problemas:

📧 **Email**: soporte-integracion@bancopagatodo.com  
📞 **Teléfono**: +52 (55) XXXX-XXXX  
⏰ **Horario**: Lunes a Viernes, 9:00 - 18:00 (hora México)  
🌐 **Portal**: https://developers.bancopagatodo.com

**Al contactar, incluye**:
- Versión de la herramienta
- Sistema operativo
- Comando exacto que ejecutaste
- Mensaje de error completo
- Client-ID (si aplica)

---

**Última actualización**: Enero 2025
