# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["RoslynStone.sln", "./"]
COPY ["src/RoslynStone.Api/RoslynStone.Api.csproj", "src/RoslynStone.Api/"]
COPY ["src/RoslynStone.Core/RoslynStone.Core.csproj", "src/RoslynStone.Core/"]
COPY ["src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj", "src/RoslynStone.Infrastructure/"]
COPY ["src/RoslynStone.ServiceDefaults/RoslynStone.ServiceDefaults.csproj", "src/RoslynStone.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/RoslynStone.Api/RoslynStone.Api.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/RoslynStone.Api"
RUN dotnet build "RoslynStone.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RoslynStone.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables for MCP server
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=

# The MCP server uses stdio transport, so we expose no HTTP ports
# Telemetry will be sent to the OTEL endpoint configured via environment variables

ENTRYPOINT ["dotnet", "RoslynStone.Api.dll"]
