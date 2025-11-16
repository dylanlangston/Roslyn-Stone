# Devcontainer Configuration

This directory contains the VS Code devcontainer configuration for Roslyn-Stone development.

## Features

- **Base Image**: Official .NET SDK 10.0 (Ubuntu Noble)
- **Docker-in-Docker**: Enabled for testing containerized scenarios
- **VS Code Extensions**: Pre-configured C# development tools
- **Build Tools**: CSharpier, dotnet-format
- **Non-root User**: Development as `vscode` user

## Docker-in-Docker Support

The devcontainer includes Docker-in-Docker (DinD) functionality, which allows you to:
- Build Docker images from within the container
- Run Docker containers for testing
- Test containerized deployments of Roslyn-Stone
- Develop and test Docker-based workflows

### Testing Docker-in-Docker

After opening the project in the devcontainer, verify Docker-in-Docker is working:

```bash
# Check Docker is available
docker --version

# Run a test container
docker run --rm hello-world

# Build a test image (example)
echo 'FROM alpine:latest' > /tmp/Dockerfile.test
echo 'CMD ["echo", "DinD works!"]' >> /tmp/Dockerfile.test
docker build -t dind-test -f /tmp/Dockerfile.test /tmp
docker run --rm dind-test
```

## Quick Start

1. Open this repository in VS Code
2. Install the "Dev Containers" extension if not already installed
3. Press `F1` and select "Dev Containers: Reopen in Container"
4. Wait for the container to build and the project to restore
5. Start developing!

## Post-Create Command

The devcontainer automatically runs `dotnet restore && dotnet build` after creation to ensure the project is ready for development.

## Customization

### Adding VS Code Extensions

Edit the `extensions` array in `devcontainer.json`:

```json
"customizations": {
  "vscode": {
    "extensions": [
      "your.extension.id"
    ]
  }
}
```

### Adding System Packages

Edit the `Dockerfile` to install additional packages:

```dockerfile
RUN apt-get update && apt-get install -y \
    your-package-name \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*
```

### Adding .NET Tools

Edit the `Dockerfile` to install global .NET tools:

```dockerfile
RUN dotnet tool install -g your-tool-name
```

## Troubleshooting

### Docker-in-Docker Not Working

If Docker commands fail inside the container:

1. Ensure Docker is running on your host machine
2. Check that the Docker socket mount is correct
3. Verify the docker-in-docker feature is enabled in `devcontainer.json`
4. Try rebuilding the container: "Dev Containers: Rebuild Container"

### Build Failures

If the post-create command fails:

1. Check internet connectivity
2. Verify NuGet feeds are accessible
3. Manually run `dotnet restore` and check for errors
4. Review the devcontainer creation logs

## References

- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
- [Docker-in-Docker Feature](https://github.com/devcontainers/features/tree/main/src/docker-in-docker)
- [.NET Dev Container Images](https://github.com/devcontainers/images/tree/main/src/dotnet)
