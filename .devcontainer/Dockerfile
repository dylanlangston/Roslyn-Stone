FROM mcr.microsoft.com/devcontainers/dotnet:latest AS base

# Important we change to the vscode user that the devcontainer runs under
USER vscode
WORKDIR /home/vscode

# Install UV
RUN curl -LsSf https://astral.sh/uv/install.sh | sh