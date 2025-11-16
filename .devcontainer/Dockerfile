# Use official .NET SDK image as base
ARG VARIANT="10.0-noble"
FROM mcr.microsoft.com/dotnet/sdk:${VARIANT}

# Install additional tools and dependencies
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
    # Common utilities
    git \
    curl \
    wget \
    unzip \
    ca-certificates \
    # Build tools
    build-essential \
    # Shell utilities
    zsh \
    bash-completion \
    # Process tools
    procps \
    lsof \
    # Network tools
    net-tools \
    iputils-ping \
    # Editor
    vim \
    nano \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user to use if it doesn't exist
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

# The base image may already have a user with UID 1000 (ubuntu), 
# so we'll either create vscode user or use the existing user
RUN if id -u $USER_UID >/dev/null 2>&1; then \
        # User with this UID exists, just ensure sudo is configured \
        EXISTING_USER=$(id -un $USER_UID); \
        apt-get update && apt-get install -y sudo \
        && echo "$EXISTING_USER ALL=(root) NOPASSWD:ALL" > /etc/sudoers.d/$EXISTING_USER \
        && chmod 0440 /etc/sudoers.d/$EXISTING_USER \
        && apt-get clean && rm -rf /var/lib/apt/lists/*; \
    else \
        # Create new user \
        groupadd --gid $USER_GID $USERNAME \
        && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME -s /bin/bash \
        && apt-get update && apt-get install -y sudo \
        && echo "$USERNAME ALL=(root) NOPASSWD:ALL" > /etc/sudoers.d/$USERNAME \
        && chmod 0440 /etc/sudoers.d/$USERNAME \
        && apt-get clean && rm -rf /var/lib/apt/lists/*; \
    fi

# Set up environment for dotnet tools (works for any user with UID 1000)
ENV PATH="${PATH}:/root/.dotnet/tools:/home/ubuntu/.dotnet/tools:/home/vscode/.dotnet/tools"

# Set the default shell to bash
ENV SHELL=/bin/bash

# Switch to the non-root user (use existing user from base image if UID matches)
USER $USER_UID

# Set working directory
WORKDIR /workspace

# Keep container running
CMD ["/bin/bash"]
