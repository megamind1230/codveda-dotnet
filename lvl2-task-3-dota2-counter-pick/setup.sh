#!/usr/bin/env bash
#
# CounterPick — one-command setup and launch
# Usage: chmod +x setup.sh && ./setup.sh
#
set -e

PROJECT="CounterPick.Api"

echo "============================================"
echo "  CounterPick — Setup & Launch"
echo "============================================"
echo ""

# ── Check prerequisites ──────────────────────────
echo "1/5  Checking prerequisites..."

if ! command -v dotnet &>/dev/null; then
    echo ""
    echo "ERROR: .NET SDK is not installed."
    echo ""
    echo "Install it from:"
    echo "  https://dotnet.microsoft.com/download/dotnet/8.0"
    echo ""
    echo "Or use your package manager:"
    echo "  Ubuntu/Debian:  sudo apt install dotnet-sdk-8.0"
    echo "  macOS (Homebrew): brew install dotnet-sdk"
    echo "  Windows:         winget install Microsoft.DotNet.SDK.8"
    echo ""
    exit 1
fi

DOTNET_VERSION=$(dotnet --version | cut -d'.' -f1)
if [ "$DOTNET_VERSION" -lt 8 ]; then
    echo "ERROR: .NET 8+ required (you have .NET $(dotnet --version))"
    exit 1
fi

echo "       .NET SDK $(dotnet --version) — OK"
echo ""

# ── Generate secure JWT key ──────────────────────
echo "2/5  Generating JWT signing key..."

cd "$(dirname "$0")"

# Initialize user-secrets if not already done
dotnet user-secrets init --project "$PROJECT" 2>/dev/null || true

# Generate a random 32-character key
if [ -f /dev/urandom ]; then
    JWT_KEY=$(tr -dc 'A-Za-z0-9!@#$%^&*()_+-=' < /dev/urandom | head -c 32)
else
    JWT_KEY=$(date +%s | sha256sum | base64 | head -c 32)
fi

dotnet user-secrets set "Jwt:Key" "$JWT_KEY" --project "$PROJECT" > /dev/null

echo "       JWT key stored securely in user-secrets"
echo ""

# ── Restore packages ─────────────────────────────
echo "3/5  Restoring NuGet packages..."
dotnet restore "$PROJECT" 2>&1 | tail -1
echo ""

# ── Build ────────────────────────────────────────
echo "4/5  Building..."
dotnet build "$PROJECT" 2>&1 | tail -1
echo ""

# ── Free up port ────────────────────────────────
if lsof -ti:5195 &>/dev/null; then
    echo "       Port 5195 in use — stopping existing process..."
    kill $(lsof -ti:5195) 2>/dev/null
    sleep 1
fi

# ── Run ──────────────────────────────────────────
echo "5/5  Launching app..."
echo ""
echo "============================================"
echo "  App is running at:"
echo "    http://localhost:5195"
echo ""
echo "  Open in your browser and click"
echo "    \"Login\" → use the admin account:"
echo ""
echo "    Username:  admin"
echo "    Password:  Dota2@Secure2024!"
echo ""
echo "  Or register your own account at:"
echo "    http://localhost:5195/login.html"
echo ""
echo "  Press Ctrl+C to stop the server."
echo "============================================"
echo ""

dotnet run --project "$PROJECT"
