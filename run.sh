#!/bin/bash
set -e

echo "🚀 GitHub Insights - Quick Start Script"
echo "========================================"

# Check prerequisites
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found"
    echo ""
    echo "Please install .NET 10 SDK:"
    echo "  macOS: brew install --cask dotnet-sdk"
    echo "  Or download from: https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

echo "✅ .NET SDK found: $(dotnet --version)"

# Check if Docker is preferred
if command -v docker &> /dev/null; then
    echo "✅ Docker found: $(docker --version)"
    echo ""
    echo "Choose deployment method:"
    echo "  1) Run with .NET SDK (native)"
    echo "  2) Run with Docker"
    read -p "Enter choice [1-2]: " choice
    
    if [ "$choice" = "2" ]; then
        echo ""
        echo "📦 Building Docker image..."
        docker build -t github-insights:latest .
        
        echo ""
        echo "🎯 Starting container..."
        docker run -d \
            --name github-insights \
            -p 5281:8080 \
            -e GitHub__Organization=microsoft \
            github-insights:latest
        
        echo ""
        echo "✅ Application running in Docker!"
        echo "   API: http://localhost:5281"
        echo "   Swagger: http://localhost:5281/api-docs"
        echo "   Health: http://localhost:5281/health"
        echo ""
        echo "Stop with: docker stop github-insights"
        echo "Remove with: docker rm github-insights"
        exit 0
    fi
fi

# Run with .NET SDK
cd src

echo ""
echo "📦 Restoring packages..."
dotnet restore

echo ""
echo "🔨 Building application..."
dotnet build --no-restore

echo ""
echo "🎯 Starting application..."
echo "   API: http://localhost:5281"
echo "   Swagger: http://localhost:5281/api-docs"
echo "   Health: http://localhost:5281/health"
echo ""
echo "Press Ctrl+C to stop"
echo ""

dotnet run --no-build
