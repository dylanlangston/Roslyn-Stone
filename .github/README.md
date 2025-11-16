# GitHub Configuration Directory

This directory contains configuration files for GitHub Copilot and other GitHub integrations.

## Structure

### `/copilot-instructions.md`
Main instructions file for GitHub Copilot coding agent. This file provides:
- Project overview and architecture
- Development setup instructions
- Coding standards and conventions
- Security considerations
- Testing guidelines
- Pull request guidelines

This is the primary reference that Copilot will use when working on any part of the repository.

### `/instructions/`
Directory containing scope-specific instruction files that provide more granular guidance for particular areas of the codebase.

#### `mcp.instructions.md`
Instructions specific to Model Context Protocol (MCP) server development:
- Scoped to C# files and MCP-related patterns
- MCP tool definition patterns
- JSON-RPC 2.0 compliance guidelines
- Security and performance considerations

#### `repl.instructions.md`
Instructions specific to C# REPL implementation:
- Scoped to C# files and REPL-related patterns
- Script execution patterns using Roslyn
- State management guidelines
- NuGet integration patterns
- Security and sandboxing requirements

#### `testing.instructions.md`
Instructions for writing and maintaining tests:
- Scoped to test files and directories
- xUnit testing patterns
- Mocking strategies
- Test data management
- Coverage requirements

### `/agents/`
Directory containing custom agent definitions for specialized tasks.

#### `CSharpExpert.agent.md`
Custom agent definition for C# and .NET expertise. This agent should be delegated to for:
- Complex C# code changes
- Roslyn-specific implementations
- .NET framework integrations
- Performance-critical code paths

## How It Works

1. **Global Instructions**: `copilot-instructions.md` provides repository-wide context and guidelines
2. **Scoped Instructions**: Files in `instructions/` directory provide additional context based on file patterns and languages
3. **Custom Agents**: Specialized agents in `agents/` directory handle domain-specific tasks

When Copilot works on a file, it:
1. Always reads the global `copilot-instructions.md`
2. Reads any applicable scoped instructions based on file path and language
3. Can delegate to custom agents for specialized work

## Best Practices

- Keep instructions focused and actionable
- Use YAML frontmatter in `.instructions.md` files to define scope
- Update instructions when patterns or standards change
- Reference custom agents in instructions when appropriate
- Keep instructions concise but comprehensive
