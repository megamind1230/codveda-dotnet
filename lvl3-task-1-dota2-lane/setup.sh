#!/usr/bin/env bash
#
# DotaLane — Setup & Run Script
#
# Usage:
#   ./setup.sh               # default: docker
#   ./setup.sh docker        # run with docker compose (Consul + RabbitMQ)
#                            #   Website → http://localhost:5010/
#                            #   $ docker compose up        # start
#                            #   $ docker compose down      # stop
#   ./setup.sh local         # run locally without Docker (less reliable)
#   ./setup.sh k8s           # deploy to Kubernetes
#   ./setup.sh stop          # stop local services
#   ./setup.sh clean         # stop + remove docker compose volumes/images
#

set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
SRC="$ROOT/src"
LOGDIR="$HOME/magnus/DotaLane/logs"

MODE="${1:-docker}"

# ──────────────────────────────────────────────
# Prerequisites
# ──────────────────────────────────────────────
check_prereqs() {
    local missing=()

    command -v dotnet &>/dev/null || missing+=("dotnet SDK (https://dotnet.microsoft.com/download)")

    if command -v dotnet &>/dev/null; then
        sdks=$(dotnet --list-sdks 2>/dev/null)
        runtimes=$(dotnet --list-runtimes 2>/dev/null)

        echo "       .NET SDKs installed:"
        while IFS= read -r line; do
            echo "         $line"
        done <<< "$sdks"

        echo "       ASP.NET runtimes installed:"
        while IFS= read -r line; do
            echo "         $line"
        done <<< "$(echo "$runtimes" | grep 'Microsoft.AspNetCore.App')"

        if ! echo "$sdks" | grep -q '^8\.'; then
            missing+=("dotnet SDK 8.0.x (project targets net8.0)")
        fi
        if ! echo "$runtimes" | grep -q 'Microsoft.AspNetCore.App 8\.'; then
            missing+=("ASP.NET Core 8.0.x runtime")
        fi
    fi

    command -v curl   &>/dev/null || missing+=("curl")

    if [[ "$MODE" == "docker" ]]; then
        command -v docker &>/dev/null || missing+=("docker")
    fi
    if [[ "$MODE" == "k8s" ]]; then
        command -v docker &>/dev/null || missing+=("docker")
        command -v kubectl &>/dev/null || missing+=("kubectl")
    fi

    if [[ ${#missing[@]} -gt 0 ]]; then
        echo ""
        echo "  [ERROR] Missing prerequisites:"
        for m in "${missing[@]}"; do
            echo "         • $m"
        done
        exit 1
    fi
}

# ──────────────────────────────────────────────
# Setup environment
# ──────────────────────────────────────────────
setup_env() {
    mkdir -p "$LOGDIR"
    echo "[INFO] Log directory: $LOGDIR"
}

# ──────────────────────────────────────────────
# Build all projects
# ──────────────────────────────────────────────
build() {
    echo ""
    echo "═══════════════════════════════════════"
    echo "  Building all projects…"
    echo "═══════════════════════════════════════"
    dotnet build "$SRC/DotaLane.slnx" --nologo -v q
    echo "[OK] Build succeeded"
}

# ──────────────────────────────────────────────
# Free ports (kill any process on service ports)
# ──────────────────────────────────────────────
free_ports() {
    for port in 5000 5001 5002 5003 5004 5005 5010; do
        fuser -k "${port}/tcp" 2>/dev/null || true
    done
    sleep 1
}

# ──────────────────────────────────────────────
# Mode: local — run services in background
# ──────────────────────────────────────────────
run_local() {
    echo ""
    echo "═══════════════════════════════════════"
    echo "  Starting services (standalone)…"
    echo "═══════════════════════════════════════"

    free_ports

    echo "[INFO] Starting HeroService on :5001"
    dotnet run --project "$SRC/HeroService" --no-build &
    PID_HERO=$!

    echo "[INFO] Starting AdviceService on :5003 (gRPC) + :5005 (health)"
    dotnet run --project "$SRC/AdviceService" --no-build &
    PID_ADVICE=$!

    sleep 3

    echo "[INFO] Starting MatchupService on :5002 (gRPC) + :5004 (REST)"
    dotnet run --project "$SRC/MatchupService" --no-build &
    PID_MATCHUP=$!

    sleep 2

    echo "[INFO] Starting ApiGateway on :5000"
    dotnet run --project "$SRC/ApiGateway" --no-build &
    PID_GATEWAY=$!

    echo "[INFO] Starting Frontend on :5010"
    dotnet run --project "$SRC/Frontend" --no-build &
    PID_FRONTEND=$!

    echo ""
    echo "$PID_HERO"  > "$ROOT/.pids"
    echo "$PID_ADVICE"  >> "$ROOT/.pids"
    echo "$PID_MATCHUP" >> "$ROOT/.pids"
    echo "$PID_GATEWAY" >> "$ROOT/.pids"
    echo "$PID_FRONTEND" >> "$ROOT/.pids"

    echo "[INFO] PIDs saved to .pids"
    echo "[INFO] Waiting for services to start…"
    sleep 8
}

stop_local() {
    if [[ -f "$ROOT/.pids" ]]; then
        echo "[INFO] Stopping local services…"
        while read -r pid; do
            kill "$pid" 2>/dev/null && echo "  stopped PID $pid" || true
        done < "$ROOT/.pids"
        rm -f "$ROOT/.pids"
    fi
    free_ports
}

# ──────────────────────────────────────────────
# Mode: docker — docker compose
# ──────────────────────────────────────────────
run_docker() {
    echo ""
    echo "═══════════════════════════════════════"
    echo "  Building and starting via Docker Compose…"
    echo "═══════════════════════════════════════"

    docker compose -f "$ROOT/docker-compose.yml" build
    docker compose -f "$ROOT/docker-compose.yml" up -d

    echo "[INFO] Waiting for services…"
    sleep 15
}

stop_docker() {
    echo "[INFO] Stopping Docker Compose services…"
    docker compose -f "$ROOT/docker-compose.yml" down
}

# ──────────────────────────────────────────────
# Mode: k8s — Kubernetes
# ──────────────────────────────────────────────
run_k8s() {
    echo ""
    echo "═══════════════════════════════════════"
    echo "  Deploying to Kubernetes…"
    echo "═══════════════════════════════════════"

    echo "[INFO] Building Docker images…"
    docker compose -f "$ROOT/docker-compose.yml" build

    echo "[INFO] Applying k8s manifests…"
    kubectl apply -f "$ROOT/k8s/"

    echo "[INFO] Waiting for pods to become ready…"
    kubectl wait --for=condition=ready pods --all --timeout=120s || true
    kubectl get pods
}

stop_k8s() {
    echo "[INFO] Deleting Kubernetes resources…"
    kubectl delete -f "$ROOT/k8s/" --ignore-not-found
}

# ──────────────────────────────────────────────
# Health checks
# ──────────────────────────────────────────────
health_checks() {
    local base="${1:-http://localhost:5000}"
    local hero="${2:-http://localhost:5001}"
    local matchup="${3:-http://localhost:5004}"
    local advice="${4:-http://localhost:5005}"

    echo ""
    echo "═══════════════════════════════════════"
    echo "  Running health checks…"
    echo "═══════════════════════════════════════"

    local ok=0 fail=0

    check_endpoint() {
        local name="$1" url="$2"
        if curl -sf --connect-timeout 3 --max-time 5 "$url" > /dev/null 2>&1; then
            echo "[✔] $name — $url"
            ((++ok))
        else
            echo "[✘] $name — $url"
            ((++fail))
        fi
    }

    check_gateway() {
        local name="$1" url="$2"
        if curl -sf --connect-timeout 3 --max-time 5 "$url" | head -c 100 > /dev/null 2>&1; then
            echo "[✔] $name — $url"
            ((++ok))
        else
            echo "[✘] $name — $url"
            ((++fail))
        fi
    }

    check_endpoint "HeroService"      "$hero/healthz"
    check_endpoint "MatchupService"   "$matchup/healthz"
    check_endpoint "AdviceService"    "$advice/healthz"
    check_gateway "  Gateway Heroes"  "$base/api/heroes"
    check_gateway "  Gateway Matchup" "$base/api/matchup/19/23?lane=safe"

    echo ""
    echo "  Results: $ok passed, $fail failed"

    if [[ "$ok" -gt 0 ]]; then
        echo ""
        echo "  🌐  Website: http://localhost:5010/"
        echo ""
        echo "  Quick test commands:"
        echo "    curl $base/api/heroes"
        echo "    curl $base/api/heroes/1"
        echo "    curl \"$base/api/matchup/19/23?lane=safe\""
        echo "    curl -X POST $hero/api/heroes/reload"
        if [[ "$MODE" == "k8s" ]]; then
            echo "    kubectl get pods"
        fi
    fi
}

# ──────────────────────────────────────────────
# Clean
# ──────────────────────────────────────────────
clean() {
    echo "[INFO] Cleaning up…"
    stop_local
    stop_docker 2>/dev/null || true
    docker compose -f "$ROOT/docker-compose.yml" down -v --rmi local --remove-orphans 2>/dev/null || true
    echo "[OK] Clean complete"
}

# ──────────────────────────────────────────────
# Main
# ──────────────────────────────────────────────
main() {
    echo ""
    echo "  ⚔  DotaLane — Pick your lane. See the matchup. Get advice."
    echo ""
    echo "  🌐  Website → http://localhost:5010/"
    echo ""

    check_prereqs

    case "$MODE" in
        local)
            stop_local
            setup_env
            build
            run_local
            health_checks
            ;;
        docker)
            setup_env
            build
            run_docker
            health_checks "http://localhost:5000" "http://localhost:5001" \
                          "http://localhost:5004" "http://localhost:5005"
            ;;
        k8s)
            setup_env
            build
            run_k8s
            health_checks "http://localhost:30000" "http://localhost:30000" \
                          "http://localhost:30000" "http://localhost:30000"
            ;;
        stop)
            stop_local
            ;;
        clean)
            clean
            ;;
        *)
            echo "Usage: $0 {local|docker|k8s|stop|clean}"
            exit 1
            ;;
    esac

    echo ""
    echo "═══════════════════════════════════════"
    echo "  Done. Logs → $LOGDIR"
    echo "  🌐  Website → http://localhost:5010/"
    echo "═══════════════════════════════════════"
}

main
