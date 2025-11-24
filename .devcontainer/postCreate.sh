#!/bin/sh

# Build the dotnet project to restore dependencies
dotnet restore && dotnet build

# setup environment using UV
uv venv
source .venv/bin/activate
echo source .venv/bin/activate >> $HOME/.bashrc

# Install huggingface_hub cli
uv pip install huggingface_hub

echo Still need to run \`huggingface-cli login\`