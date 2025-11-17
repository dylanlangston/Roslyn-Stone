# Devcontainer Configuration

This directory contains the VS Code devcontainer configuration for Roslyn-Stone development.

## Features

- **Base Image**: Official Microsoft .NET devcontainer with .NET SDK 10.0
- **Docker-in-Docker**: Docker feature enabled via devcontainer features
- **VS Code Extensions**: Pre-configured C# development tools
- **Non-root User**: Development as `vscode` user with Docker access
- **.NET Configuration**: Optimized environment variables for development

## Docker-in-Docker Support

The devcontainer includes the docker-in-docker feature which provides full Docker support, allowing you to:
- Build Docker images from within the container
- Run Docker containers for testing
- Test containerized deployments of Roslyn-Stone
- Develop and test Docker-based workflows

### Testing Docker-in-Docker

After opening the project in the devcontainer, verify Docker is working:

```bash
# Check Docker is available
docker --version

# Run a test container
docker run --rm hello-world

# Build and run a test image
echo 'FROM alpine:latest' > /tmp/Dockerfile.test
echo 'CMD ["echo", "Docker-in-Docker is working!"]' >> /tmp/Dockerfile.test
docker build -t dind-test -f /tmp/Dockerfile.test /tmp
docker run --rm dind-test
docker rmi dind-test
```

## Quick Start

1. Open this repository in VS Code
2. Install the "Dev Containers" extension if not already installed
3. Press `F1` and select "Dev Containers: Reopen in Container"
4. Wait for the container to build and the project to restore
5. Start developing!

## Configuration Details

### Dockerfile

The Dockerfile:
- Uses `mcr.microsoft.com/devcontainers/dotnet:1-10.0` as the base image (includes .NET 10.0 SDK)
- Base image already includes the `vscode` user and common development tools
- Docker will be installed by the docker-in-docker feature from `devcontainer.json`
- Minimal and clean - relies on official Microsoft devcontainer images

### devcontainer.json

The devcontainer configuration:
- Uses the docker-in-docker feature for full Docker support
- Runs the container as the `vscode` user
- Sets .NET environment variables to optimize development experience
- Configures VS Code extensions for C#, Docker, and GitHub integration
- Runs `dotnet restore && dotnet build` after container creation

## Environment Variables

The following .NET environment variables are configured:
- `DOTNET_CLI_TELEMETRY_OPTOUT=1` - Disables telemetry
- `DOTNET_GENERATE_ASPNET_CERTIFICATE=0` - Skips HTTPS dev certificate
- `DOTNET_NOLOGO=1` - Suppresses .NET logo output
- `DOTNET_USE_POLLING_FILE_WATCHER=1` - Uses polling for file watching (better for containers)

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
    && apt-get clean && rm -rf /var/lib/apt/lists/*
```

### Adding .NET Tools

Install tools in the post-create command or manually after container starts:

```bash
dotnet tool install -g your-tool-name
```

## Troubleshooting

### Docker-in-Docker Not Working

If Docker commands fail inside the container:

1. Ensure Docker is running on your host machine
2. Check that the docker-in-docker feature is enabled in `devcontainer.json`
3. Try rebuilding the container: "Dev Containers: Rebuild Container"
4. Check Docker daemon status: `sudo service docker status` or `docker info`
5. Verify the `vscode` user has Docker access: `groups` (should include `docker`)

### Build Failures

If the post-create command fails:

1. Check internet connectivity
2. Verify NuGet feeds are accessible
3. Manually run `dotnet restore` and check for errors
4. Review the devcontainer creation logs

### Permission Issues

If you encounter permission issues with Docker:

1. Verify the `vscode` user is in the `docker` group: `groups`
2. Rebuild the container to ensure the docker-in-docker feature is properly configured
3. Try restarting the Docker daemon if needed

## References

- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
- [Docker-in-Docker Feature](https://github.com/devcontainers/features/tree/main/src/docker-in-docker)
- [.NET Dev Container Images](https://github.com/devcontainers/images/tree/main/src/dotnet)
- [Microsoft Devcontainers Documentation](https://containers.dev/)
