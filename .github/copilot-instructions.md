# Copilot Instructions for Roslyn-Stone

## Project Overview

Roslyn-Stone is a developer- and LLM-friendly sandbox for C# code targeting MCP (Model Context Protocol). The project provides:
- C# REPL (Read-Eval-Print Loop) functionality
- Actionable error feedback
- XML documentation support
- NuGet extensibility for LLMs
- Integration with Model Context Protocol for AI-assisted development

## Architecture

This is a .NET-based project that leverages:
- **Microsoft Roslyn**: The .NET Compiler Platform for C# code analysis and compilation
- **Model Context Protocol (MCP)**: An open protocol for standardizing AI agent interactions
- **NuGet**: For package management and extensibility

## Development Setup

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or Rider

### Building the Project
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests (if available)
dotnet test
```

## Coding Standards

### C# Conventions
- Follow standard .NET naming conventions (PascalCase for public members, camelCase for private fields)
- Use nullable reference types where appropriate
- Prefer async/await patterns for I/O operations
- Include XML documentation comments for public APIs

### Error Handling
- Provide actionable error messages that LLMs can understand and act upon
- Include context in error messages (what was attempted, why it failed, how to fix)
- Use structured error responses when possible

### Code Quality
- Write clean, readable code with minimal complexity
- Keep methods focused and single-purpose
- Use dependency injection where appropriate
- Follow SOLID principles

## Testing Guidelines

- Write unit tests for new functionality
- Ensure tests are deterministic and isolated
- Test error conditions and edge cases
- Mock external dependencies appropriately

## MCP Integration

When working with MCP-related code:
- Follow the Model Context Protocol specification
- Ensure tools/endpoints are discoverable via JSON-RPC 2.0
- Implement proper schema definitions for all exposed operations
- Handle versioning and backwards compatibility
- Validate inputs and sanitize paths for security

## Security Considerations

- Never commit secrets or credentials
- Validate and sanitize all user inputs
- Use secure defaults for configuration
- Be cautious with dynamic code compilation
- Implement proper access controls for file system operations

## LLM-Friendly Features

When implementing features:
- Provide clear, structured error messages
- Include context and suggestions in responses
- Make APIs self-documenting through XML comments
- Support discoverability (reflection, metadata)
- Return actionable feedback that can guide next steps

## Documentation

- Keep README.md up to date with setup instructions
- Document any new APIs with XML comments
- Include usage examples for complex features
- Document breaking changes clearly

## Pull Request Guidelines

- Keep changes focused and atomic
- Include tests for new functionality
- Update documentation as needed
- Ensure all tests pass before submitting
- Use descriptive commit messages

## Custom Agents

This repository has a CSharpExpert custom agent (`.github/agents/CSharpExpert.agent.md`) that should be used for:
- Complex C# code changes
- Roslyn-specific implementations
- .NET framework integrations
- Performance-critical code paths

Delegate to the CSharpExpert agent when working on core C# functionality.
