#!/usr/bin/env bash
set -euo pipefail

APP_NAME="FaceRank"
ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
WEB_DIR="$ROOT_DIR/src/FaceRank.Web"
FUNC_DIR="$ROOT_DIR/src/FaceRank.Functions"

say() { printf "\033[1;32m%s\033[0m\n" "$*"; }
err() { printf "\033[1;31m%s\033[0m\n" "$*" >&2; }

# -----------------------------------------------------------
say "=== $APP_NAME — Quick Start ==="
say ""

# 1. Check .NET
if ! command -v dotnet &>/dev/null; then
    err "dotnet not found. Install .NET SDK: https://dotnet.microsoft.com/download"
    exit 1
fi
say "[OK] dotnet $(dotnet --version)"

# 2. Restore & build
say "Restoring and building solution..."
dotnet restore "$ROOT_DIR/FaceRank.slnx"
dotnet build "$ROOT_DIR/FaceRank.slnx" --no-restore
say "[OK] Build succeeded"

# 3. Run web app
say ""
say "Starting $APP_NAME web app..."
say "  Open http://localhost:5153 in your browser"
say "  Press Ctrl+C to stop"
say ""
cd "$WEB_DIR" && dotnet run --no-build -- --seed
