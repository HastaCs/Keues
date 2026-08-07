#!/usr/bin/env bash
#
# Genera el fichero OpenAPI (docs/openapi.json) a partir de la propia API.
#
# 1. Compila el proyecto.
# 2. Arranca la API en Development (perfil http, puerto 5125).
# 3. Descarga /openapi/v1.json a docs/openapi.json.
# 4. Detiene la API.
#
# Uso: ./scripts/export-openapi.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
PROJECT_DIR="$ROOT_DIR/Keues.API"
OUTPUT_DIR="$ROOT_DIR/docs"
OUTPUT_FILE="$OUTPUT_DIR/openapi.json"
URL="http://localhost:5125/openapi/v1.json"
PORT=5125

mkdir -p "$OUTPUT_DIR"

echo "==> Compilando Keues.API..."
dotnet build "$PROJECT_DIR" -c Debug > /dev/null

DLL="$PROJECT_DIR/bin/Debug/net10.0/Keues.API.dll"
if [ ! -f "$DLL" ]; then
  echo "ERROR: no se encontró $DLL" >&2
  exit 1
fi

echo "==> Arrancando la API en http://localhost:$PORT (Development)..."
(cd "$PROJECT_DIR" && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:$PORT" dotnet "$DLL") &
SERVER_PID=$!

cleanup() {
  echo "==> Deteniendo la API..."
  kill "$SERVER_PID" 2>/dev/null || true
  wait "$SERVER_PID" 2>/dev/null || true
}
trap cleanup EXIT

echo "==> Esperando a que el documento OpenAPI esté disponible..."
for _ in $(seq 1 60); do
  if curl -sf -o /dev/null "$URL"; then
    break
  fi
  if ! kill -0 "$SERVER_PID" 2>/dev/null; then
    echo "ERROR: el servidor terminó antes de servir el documento." >&2
    exit 1
  fi
  sleep 1
done

if ! curl -sf "$URL" > "$OUTPUT_FILE"; then
  echo "ERROR: no se pudo descargar $URL" >&2
  exit 1
fi

echo "==> Documento generado en $OUTPUT_FILE ($(wc -c < "$OUTPUT_FILE") bytes)"
