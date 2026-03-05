#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SPEC_DIR="$REPO_ROOT/specs"
GENERATED_DIR="$REPO_ROOT/src/Tripletex.Api/Generated"

echo "=== Step 1: Download OpenAPI spec ==="
mkdir -p "$SPEC_DIR"
if [ ! -f "$SPEC_DIR/tripletex-openapi-original.json" ] || [ "${FORCE_DOWNLOAD:-}" = "1" ]; then
    curl -sL "https://tripletex.no/v2/openapi.json" -o "$SPEC_DIR/tripletex-openapi-original.json"
    echo "Downloaded spec"
else
    echo "Using cached spec (set FORCE_DOWNLOAD=1 to refresh)"
fi

echo "=== Step 2: Preprocess spec ==="
dotnet run --project "$REPO_ROOT/tools/Tripletex.SpecPreprocessor" -- \
    "$SPEC_DIR/tripletex-openapi-original.json" \
    "$SPEC_DIR/tripletex-openapi-processed.json" \
    "$GENERATED_DIR/PathMappings.g.cs"

echo "=== Step 3: Generate Kiota client ==="
if ! command -v kiota &> /dev/null; then
    echo "Installing Kiota CLI..."
    dotnet tool install --global Microsoft.OpenApi.Kiota 2>/dev/null || true
fi

rm -rf "$GENERATED_DIR/Client"

kiota generate \
    --language CSharp \
    --class-name TripletexGeneratedClient \
    --namespace-name Tripletex.Api.Generated \
    --openapi "$SPEC_DIR/tripletex-openapi-processed.json" \
    --output "$GENERATED_DIR/Client" \
    --type-access-modifier internal \
    --log-level warning \
    --exclude-backward-compatible

echo "=== Step 4: Build ==="
dotnet build "$REPO_ROOT/src/Tripletex.Api/Tripletex.Api.csproj"

echo "=== Done ==="
echo "Generated client in $GENERATED_DIR/Client"
