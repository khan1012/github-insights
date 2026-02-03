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
        
        # Check if GITHUB_TOKEN is set
        if [ -z "$GITHUB_TOKEN" ]; then
            echo "⚠️  GITHUB_TOKEN environment variable is not set"
            echo ""
            echo "Without a token, you'll be limited to 60 API requests/hour."
            echo "With a token, you get 5,000 API requests/hour."
            echo ""
            echo "To set the token:"
            echo "  Git Bash:      export GITHUB_TOKEN=ghp_your_token"
            echo "  PowerShell:    \$env:GITHUB_TOKEN=\"ghp_your_token\""
            echo ""
            read -p "Continue without token? [y/N]: " continue_choice
            
            if [[ ! "$continue_choice" =~ ^[Yy]$ ]]; then
                echo "❌ Cancelled. Please set GITHUB_TOKEN and try again."
                exit 1
            fi
        else
            echo "✅ GITHUB_TOKEN is set"
        fi
        
        echo ""
        echo "📦 Building Docker image..."
        docker build -t github-insights:latest .
        
        echo ""
        echo "🎯 Starting container..."
        docker run -d \
            --name github-insights \
            -p 5281:8080 \
            -e ASPNETCORE_ENVIRONMENT=Development \
            -e GitHub__Organization=microsoft \
            -e GitHub__Token="${GITHUB_TOKEN:-}" \
            github-insights:latest
        
        echo ""
        echo "✅ Application running in Docker!"
        echo "   API: http://localhost:5281"
        echo "   Scalar: http://localhost:5281/scalar/v1"
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
echo "   Scalar: http://localhost:5281/scalar/v1"
echo "   Health: http://localhost:5281/health"
echo ""
echo "Press Ctrl+C to stop"
echo ""

dotnet run --no-build
