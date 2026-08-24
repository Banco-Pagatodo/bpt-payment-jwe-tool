#!/bin/bash

# Script de verificación de BPT.JWE.Tool
# Ejecuta una serie de tests para asegurar que todo funciona correctamente

echo "============================================="
echo "BPT JWE Tool - Verificación de Instalación"
echo "============================================="
echo ""

# Colores
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Contadores
PASSED=0
FAILED=0

# Función para imprimir resultado
check_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓ PASS${NC} - $2"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAIL${NC} - $2"
        ((FAILED++))
    fi
}

echo "1. Verificando .NET SDK..."
if command -v dotnet &> /dev/null; then
    VERSION=$(dotnet --version)
    echo "   .NET SDK instalado: $VERSION"
    check_result 0 "dotnet está instalado"
else
    check_result 1 "dotnet NO está instalado"
fi
echo ""

echo "2. Verificando estructura de archivos..."

# Archivos principales
FILES=(
    "BPT.JWE.Tool.csproj"
    "Program.cs"
    "Services/JweService.cs"
    "Models/JweConfig.cs"
    "Models/ClientData.cs"
    "Examples/cashin-request.json"
    "Examples/cashout-request.json"
    "README.md"
    "USAGE.md"
    "FAQ.md"
    ".gitignore"
)

for file in "${FILES[@]}"; do
    if [ -f "$file" ]; then
        check_result 0 "Existe: $file"
    else
        check_result 1 "Falta: $file"
    fi
done
echo ""

echo "3. Verificando compilación..."
if dotnet build > /dev/null 2>&1; then
    check_result 0 "El proyecto compila sin errores"
else
    check_result 1 "El proyecto tiene errores de compilación"
    dotnet build
fi
echo ""

echo "4. Verificando comandos CLI..."

# Test: help
if dotnet run -- --help > /dev/null 2>&1; then
    check_result 0 "Comando 'help' funciona"
else
    check_result 1 "Comando 'help' falla"
fi

# Test: generate-keys help
if dotnet run -- generate-keys --help > /dev/null 2>&1; then
    check_result 0 "Comando 'generate-keys' existe"
else
    check_result 1 "Comando 'generate-keys' no existe"
fi

# Test: configure help
if dotnet run -- configure --help > /dev/null 2>&1; then
    check_result 0 "Comando 'configure' existe"
else
    check_result 1 "Comando 'configure' no existe"
fi

# Test: encrypt help
if dotnet run -- encrypt --help > /dev/null 2>&1; then
    check_result 0 "Comando 'encrypt' existe"
else
    check_result 1 "Comando 'encrypt' no existe"
fi

# Test: decrypt help
if dotnet run -- decrypt --help > /dev/null 2>&1; then
    check_result 0 "Comando 'decrypt' existe"
else
    check_result 1 "Comando 'decrypt' no existe"
fi
echo ""

echo "5. Test funcional: Generar llaves de prueba..."
rm -rf ./verify-test-keys
if dotnet run -- generate-keys --output-dir ./verify-test-keys > /dev/null 2>&1; then
    if [ -f "./verify-test-keys/client-rsa-private.pem" ] && \
       [ -f "./verify-test-keys/client-rsa-public.pem" ] && \
       [ -f "./verify-test-keys/client-ecdsa-private.pem" ] && \
       [ -f "./verify-test-keys/client-ecdsa-public.pem" ]; then
        check_result 0 "Generación de llaves funciona correctamente"
    else
        check_result 1 "Generación de llaves incompleta"
    fi
else
    check_result 1 "Generación de llaves falla"
fi
echo ""

echo "6. Test funcional: Configuración..."
if [ -d "./verify-test-keys" ]; then
    # Crear llaves "del banco" (copia de las del cliente para test)
    mkdir -p ./verify-bank-keys
    cp ./verify-test-keys/client-rsa-public.pem ./verify-bank-keys/bank-rsa-public.pem
    cp ./verify-test-keys/client-ecdsa-public.pem ./verify-bank-keys/bank-ecdsa-public.pem
    
    if dotnet run -- configure \
        --client-id "VERIFY_TEST" \
        --client-rsa-private "./verify-test-keys/client-rsa-private.pem" \
        --client-ecdsa-private "./verify-test-keys/client-ecdsa-private.pem" \
        --bank-rsa-public "./verify-bank-keys/bank-rsa-public.pem" \
        --bank-ecdsa-public "./verify-bank-keys/bank-ecdsa-public.pem" \
        --environment Development > /dev/null 2>&1; then
        check_result 0 "Configuración funciona correctamente"
    else
        check_result 1 "Configuración falla"
    fi
fi
echo ""

echo "7. Test funcional: Cifrado..."
if [ -f "appsettings.Development.json" ]; then
    if dotnet run -- encrypt \
        --input ./Examples/cashin-request.json \
        --output ./verify-encrypted.txt \
        --environment Development > /dev/null 2>&1; then
        if [ -f "./verify-encrypted.txt" ] && [ -s "./verify-encrypted.txt" ]; then
            check_result 0 "Cifrado funciona correctamente"
            
            # Verificar que el archivo no está vacío y tiene formato de JWE
            FIRST_CHARS=$(head -c 10 ./verify-encrypted.txt)
            if [[ $FIRST_CHARS == eyJ* ]]; then
                check_result 0 "El JWE tiene formato válido (comienza con 'eyJ')"
            else
                check_result 1 "El JWE no tiene formato válido"
            fi
        else
            check_result 1 "Archivo cifrado está vacío"
        fi
    else
        check_result 1 "Cifrado falla"
    fi
fi
echo ""

echo "8. Verificando ejemplos JSON..."
if command -v jq &> /dev/null; then
    if jq . ./Examples/cashin-request.json > /dev/null 2>&1; then
        check_result 0 "cashin-request.json es JSON válido"
    else
        check_result 1 "cashin-request.json tiene errores"
    fi
    
    if jq . ./Examples/cashout-request.json > /dev/null 2>&1; then
        check_result 0 "cashout-request.json es JSON válido"
    else
        check_result 1 "cashout-request.json tiene errores"
    fi
else
    echo -e "${YELLOW}⚠ jq no instalado - saltando validación de JSON${NC}"
fi
echo ""

echo "9. Limpiando archivos de prueba..."
rm -rf ./verify-test-keys ./verify-bank-keys ./verify-encrypted.txt
check_result 0 "Limpieza completada"
echo ""

# Resumen
echo "============================================="
echo "RESUMEN"
echo "============================================="
echo -e "${GREEN}Tests Pasados: $PASSED${NC}"
echo -e "${RED}Tests Fallados: $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ ¡Todas las verificaciones pasaron!${NC}"
    echo ""
    echo "La herramienta está lista para usar."
    echo ""
    echo "Próximos pasos:"
    echo "  1. Genera tus llaves reales: dotnet run -- generate-keys"
    echo "  2. Configura la herramienta: dotnet run -- configure ..."
    echo "  3. Consulta USAGE.md para ejemplos de uso"
    exit 0
else
    echo -e "${RED}✗ Algunas verificaciones fallaron${NC}"
    echo ""
    echo "Por favor revisa los errores arriba."
    echo "Consulta FAQ.md para troubleshooting."
    exit 1
fi
