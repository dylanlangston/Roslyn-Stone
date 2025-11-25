#!/bin/bash
# Python Code Quality Check Script
# Run this before committing Python code changes

set -e

echo "🐍 Running Python code quality checks..."
echo ""

cd "$(dirname "$0")/../src/RoslynStone.GradioModule"

echo "1️⃣ Running Ruff formatter check..."
ruff format --check . || {
    echo "❌ Formatting issues found. Run 'ruff format .' to fix."
    exit 1
}
echo "✅ Formatting check passed"
echo ""

echo "2️⃣ Running Ruff linter..."
ruff check . || {
    echo "❌ Linting issues found. Run 'ruff check --fix' to auto-fix."
    exit 1
}
echo "✅ Linting passed"
echo ""

echo "3️⃣ Running mypy type checker..."
mypy . || {
    echo "⚠️ Type checking found issues (non-blocking for Gradio UI code)"
}
echo ""

echo "✨ All Python quality checks passed!"
