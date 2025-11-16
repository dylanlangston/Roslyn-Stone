FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0 AS base

# The base image already has .NET 10.0, vscode user, and common dev tools
# Just ensure vscode user is in the docker group (will be added by docker-in-docker feature)
# We don't need to install Docker here - it will be added by the feature

# Set up environment for dotnet tools
ENV PATH="${PATH}:/home/vscode/.dotnet/tools"

# Set working directory
WORKDIR /workspaces
