#!/bin/bash
set -e

# Create solution
dotnet new sln -n RoslynStone

# Create projects
dotnet new webapi -n RoslynStone.Api -o src/RoslynStone.Api --framework net9.0
dotnet new classlib -n RoslynStone.Core -o src/RoslynStone.Core --framework net9.0
dotnet new classlib -n RoslynStone.Infrastructure -o src/RoslynStone.Infrastructure --framework net9.0
dotnet new xunit -n RoslynStone.Tests -o tests/RoslynStone.Tests --framework net9.0

# Add projects to solution
dotnet sln add src/RoslynStone.Api/RoslynStone.Api.csproj
dotnet sln add src/RoslynStone.Core/RoslynStone.Core.csproj
dotnet sln add src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj
dotnet sln add tests/RoslynStone.Tests/RoslynStone.Tests.csproj

# Add project references
dotnet add src/RoslynStone.Api/RoslynStone.Api.csproj reference src/RoslynStone.Core/RoslynStone.Core.csproj
dotnet add src/RoslynStone.Api/RoslynStone.Api.csproj reference src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj
dotnet add src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj reference src/RoslynStone.Core/RoslynStone.Core.csproj
dotnet add tests/RoslynStone.Tests/RoslynStone.Tests.csproj reference src/RoslynStone.Core/RoslynStone.Core.csproj
dotnet add tests/RoslynStone.Tests/RoslynStone.Tests.csproj reference src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj

echo "Solution structure created successfully!"
