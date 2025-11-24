# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for better layer caching
COPY ["RoslynStone.sln", "./"]
COPY ["src/RoslynStone.Api/RoslynStone.Api.csproj", "src/RoslynStone.Api/"]
COPY ["src/RoslynStone.Core/RoslynStone.Core.csproj", "src/RoslynStone.Core/"]
COPY ["src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj", "src/RoslynStone.Infrastructure/"]
COPY ["src/RoslynStone.ServiceDefaults/RoslynStone.ServiceDefaults.csproj", "src/RoslynStone.ServiceDefaults/"]
COPY ["src/RoslynStone.GradioModule/RoslynStone.GradioModule.csproj", "src/RoslynStone.GradioModule/"]

# Restore dependencies (this layer will be cached if project files don't change)
RUN dotnet restore "src/RoslynStone.Api/RoslynStone.Api.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/RoslynStone.Api"
# Build into the standard bin/$(Configuration)/$(TargetFramework) output which publish expects
RUN dotnet build "RoslynStone.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

# Install CSnakes.Stage tool for Python environment setup
RUN dotnet tool install --global CSnakes.Stage
ENV PATH="/root/.dotnet/tools:${PATH}"

# Set up Python environment with CSnakes
# This downloads Python 3.12 redistributable and creates a venv
RUN setup-python --python 3.12 --venv /app/.venv --verbose

# Install UV (fast Python package installer) - only needed in build stage
RUN curl -LsSf https://astral.sh/uv/install.sh | sh
ENV PATH="/root/.local/bin:${PATH}"

# Install Gradio and dependencies using UV into the venv
# This happens at build time, not runtime
WORKDIR "/src/src/RoslynStone.GradioModule"
RUN uv pip install "gradio>=5.0.0,<6.0.0" --python /app/.venv/bin/python

# Verify Gradio is installed correctly
RUN /app/.venv/bin/python3 -c "import gradio; print(f'Gradio {gradio.__version__} installed successfully')"

# Publish the application
WORKDIR "/src/src/RoslynStone.Api"
RUN dotnet publish "RoslynStone.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser -m -d /home/appuser appuser

# Copy published application
COPY --from=publish /app/publish .
COPY src/RoslynStone.Api/entrypoint.sh .
RUN chmod +x entrypoint.sh

# Copy CSnakes Python redistributable from build stage
COPY --from=publish /root/.config/CSnakes /home/appuser/.config/CSnakes

# Some venv scripts created during the publish stage contain shebangs
# that reference the root user's CSnakes path (/root/.config/CSnakes/...).
# Create a root-side symlink to the copied location so those shebangs
# keep working in the runtime image.
RUN mkdir -p /root/.config \
    && ln -s /home/appuser/.config/CSnakes /root/.config/CSnakes || true

# Copy Python virtual environment with pre-installed Gradio from build stage
COPY --from=publish /app/.venv .venv

# Verify venv was copied correctly
RUN test -f /app/.venv/bin/python3 && test -f /app/.venv/bin/pip || \
    (echo "ERROR: Virtual environment not copied correctly!" && exit 1)

# Create symlink for backward compatibility (some code may reference /app/venv)
RUN ln -s /app/.venv /app/venv
# Change ownership to non-root user
RUN chown -R appuser:appuser /app /home/appuser/.config

# Set environment variables for MCP server and Python
# MCP_TRANSPORT: "stdio" (default) or "http"
# When using HTTP transport, set ASPNETCORE_URLS to configure listening address
ENV DOTNET_ENVIRONMENT=Production \
    MCP_TRANSPORT=stdio \
    ASPNETCORE_URLS= \
    DOTNET_EnableDiagnostics=0 \
    LD_LIBRARY_PATH=/home/appuser/.config/CSnakes/python3.12.9/python/install/lib \
    PYTHONHOME=/home/appuser/.config/CSnakes/python3.12.9/python/install

# Switch to non-root user
USER appuser

# The MCP server supports both stdio and HTTP transports
# - Stdio (default): Set MCP_TRANSPORT=stdio, no ports exposed
# - HTTP: Set MCP_TRANSPORT=http and ASPNETCORE_URLS=http://+:8080, then EXPOSE 8080
#   In HTTP mode, Gradio landing page will be available at root (/)
# Telemetry will be sent to the OTEL endpoint configured via environment variables

# Example for HTTP mode:
# ENV MCP_TRANSPORT=http
# ENV ASPNETCORE_URLS=http://+:8080
# EXPOSE 8080

ENTRYPOINT ["./entrypoint.sh"]

