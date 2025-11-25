#!/bin/bash
# Python Code Formatting and Auto-fix Script
# Automatically format and fix Python code issues

set -e

echo "🔧 Formatting and fixing Python code..."
echo ""

cd "$(dirname "$0")/../src/RoslynStone.GradioModule"

echo "1️⃣ Running Ruff formatter..."
ruff format .
echo "✅ Code formatted"
echo ""

echo "2️⃣ Running Ruff auto-fix..."
ruff check --fix --unsafe-fixes .
echo "✅ Auto-fixable issues resolved"
echo ""

echo "✨ Python code formatted and fixed!"
echo ""
echo "Run './scripts/check-python-quality.sh' to verify all checks pass."
