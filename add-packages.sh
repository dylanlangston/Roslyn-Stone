#!/bin/bash
set -e

# Add Roslyn scripting packages to Infrastructure
dotnet add src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj package Microsoft.CodeAnalysis.CSharp.Scripting
dotnet add src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj package Microsoft.CodeAnalysis.CSharp.Workspaces

# Add MCP SDK to API (we'll use the ModelContextProtocol package if available, or create interface)
# For now, we'll create our own MCP protocol implementation based on the spec

# Add additional packages
dotnet add src/RoslynStone.Api/RoslynStone.Api.csproj package Swashbuckle.AspNetCore
dotnet add src/RoslynStone.Core/RoslynStone.Core.csproj package System.Reflection.Metadata

echo "Packages added successfully!"
