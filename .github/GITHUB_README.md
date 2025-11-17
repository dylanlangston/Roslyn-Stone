# GitHub Configuration Directory

This directory contains configuration files for GitHub Copilot and other GitHub integrations.

## Structure

### `/workflows/copilot-setup-steps.yml`
Custom environment setup workflow for GitHub Copilot coding agent. This workflow:
- Installs .NET 10 SDK for latest C# features
- Installs CSharpier for code formatting
- Installs ReSharper Command Line Tools for code analysis
- Installs Cake for build automation
- Caches NuGet packages for faster subsequent runs
- Runs automatically when the workflow file is modified

See `COPILOT_ENVIRONMENT.md` for detailed documentation on the custom environment setup.

### `/COPILOT_ENVIRONMENT.md`
Detailed documentation about the custom Copilot environment:
- Overview of installed tools and their versions
- How the environment setup works
- Customization instructions
- Troubleshooting guide
- References and links to official documentation

This ensures Copilot has all necessary tools available when working on C# code.

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

#### `csharp-mcp-server.instructions.md`
Instructions specific to building Model Context Protocol (MCP) servers using the C# SDK:
- Scoped to C# and .csproj files
- C# MCP SDK best practices (ModelContextProtocol NuGet packages)
- Tool and prompt implementation patterns
- Server setup with dependency injection
- Common code patterns and examples

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
Custom agent definition for C# and .NET expertise with functional programming focus. This agent should be delegated to for:
- Complex C# code changes
- Roslyn-specific implementations
- .NET framework integrations
- Performance-critical code paths
- Functional programming refactorings (LINQ, pure functions, etc.)

### `/chatmodes/`
Directory containing chat mode definitions for specialized conversational contexts.

#### `csharp-mcp-expert.chatmode.md`
Expert chat mode for C# MCP server development:
- World-class expertise in ModelContextProtocol SDK
- Deep knowledge of .NET architecture and async programming
- Best practices for tool design and LLM-friendly interfaces
- Provides complete, production-ready code examples

### `/prompts/`
Directory containing reusable prompt templates for common tasks.

#### `csharp-mcp-server-generator.prompt.md`
Prompt template for generating complete C# MCP server projects:
- Generates project structure with proper configuration
- Includes tools, prompts, and error handling
- Provides testing guidance and troubleshooting tips
- Production-ready with comprehensive documentation

## How It Works

1. **Global Instructions**: `copilot-instructions.md` provides repository-wide context and guidelines
2. **Scoped Instructions**: Files in `instructions/` directory provide additional context based on file patterns and languages
3. **Custom Agents**: Specialized agents in `agents/` directory handle domain-specific tasks
4. **Chat Modes**: Conversational modes in `chatmodes/` directory provide expert assistance for specific scenarios
5. **Prompts**: Reusable templates in `prompts/` directory for generating code or completing common tasks

When Copilot works on a file, it:
1. Always reads the global `copilot-instructions.md`
2. Reads any applicable scoped instructions based on file path and language
3. Can delegate to custom agents for specialized work
4. Can switch to expert chat modes for conversational assistance
5. Can use prompt templates to generate code or structures

## Best Practices

- Keep instructions focused and actionable
- Use YAML frontmatter in `.instructions.md` files to define scope
- Update instructions when patterns or standards change
- Reference custom agents in instructions when appropriate
- Keep instructions concise but comprehensive
