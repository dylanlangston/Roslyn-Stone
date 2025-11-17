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

### Functional Programming Patterns

This project favors **functional programming patterns over heavy OOP abstractions**:

**Prefer:**
- **LINQ functional composition** over imperative loops
  ```csharp
  // Good: Functional with LINQ
  var results = items.Select(x => Transform(x)).Where(x => x.IsValid).ToList();
  
  // Avoid: Imperative with loops
  var results = new List<Result>();
  foreach (var item in items) {
      var transformed = Transform(item);
      if (transformed.IsValid) results.Add(transformed);
  }
  ```

- **Pure functions** for transformations and utilities
  ```csharp
  // Good: Pure function with no side effects
  public static CompilationError ToCompilationError(this Diagnostic diagnostic) =>
      new() { Code = diagnostic.Id, Message = diagnostic.GetMessage(), ... };
  ```

- **Direct service calls** from MCP Tools
  ```csharp
  // Good: Direct call
  var result = await nugetService.SearchPackagesAsync(query, skip, take, ct);
  
  // Avoid: Unnecessary abstraction layers (Commands/Queries/Handlers)
  ```

- **Expression-bodied members** and **pattern matching**
- **Records** for immutable data models where appropriate
- **Extension methods** for functional helpers

**Architecture:**
- MCP Tools call services directly: `Tool → Service`
- No CQRS pattern (Commands/Queries/Handlers removed)
- Services use LINQ and functional composition
- Functional helpers in `Infrastructure/Functional/` for pure utilities

### Error Handling
- Provide actionable error messages that LLMs can understand and act upon
- Include context in error messages (what was attempted, why it failed, how to fix)
- Use structured error responses when possible
- **Use specific exception types** instead of generic `catch (Exception)`
- Filter out `OperationCanceledException` from generic catches: `catch (Exception ex) when (ex is not OperationCanceledException)`

### Code Quality
- Write clean, readable code with minimal complexity
- Keep methods focused and single-purpose
- Use dependency injection where appropriate
- Follow SOLID principles

### Code Formatting and Analysis

**ALWAYS run these tools before committing code:**

1. **ReSharper Inspection**: Run `jb inspectcode RoslynStone.sln --output=/tmp/resharper-output.xml --verbosity=WARN` to check for code quality issues
   - Fix ALL warnings and errors before committing
   - Check the output with: `cat /tmp/resharper-output.xml | jq -r '.runs[0].results[] | select(.level == "warning" or .level == "error")'`
   - No ReSharper warnings or errors are allowed in the codebase

2. **CSharpier Formatting**: Run `csharpier format .` to format all C# files
   - This ensures consistent code formatting across the project
   - Run after making code changes and before committing

**Workflow:**
```bash
# After making code changes:
# 1. Run ReSharper and fix all issues
jb inspectcode RoslynStone.sln --output=/tmp/resharper-output.xml --verbosity=WARN
cat /tmp/resharper-output.xml | jq -r '.runs[0].results[] | select(.level == "warning" or .level == "error")'

# 2. Run CSharpier to format code
csharpier format .

# 3. Build and test
dotnet build
dotnet test

# 4. Then commit via report_progress
```

## Using Roslyn-Stone MCP Tools (Dogfooding)

**IMPORTANT**: When working on this repository, you have access to the roslyn-stone MCP server tools. Use these tools to validate C# code, test expressions, and look up documentation whenever possible.

### Available MCP Tools

#### EvaluateCsharp
Execute C# code in a stateful REPL. Use this to:
- Test C# expressions and validate they work as expected
- Verify code snippets before adding them to the codebase
- Experiment with .NET APIs and library methods
- Check return values and side effects of code
- Validate that refactorings produce the same results

Example usage:
```
EvaluateCsharp: { code: "var x = 10; x * 2" }
// Returns: { success: true, returnValue: 20, ... }
```

#### ValidateCsharp
Validate C# syntax without executing. Use this to:
- Check syntax before executing potentially problematic code
- Validate code snippets in documentation or comments
- Verify that code will compile before adding it
- Understand compilation errors with detailed diagnostics

Example usage:
```
ValidateCsharp: { code: "int x = 10; x * 2" }
// Returns: { isValid: true, issues: [] }
```

#### GetDocumentation
Look up XML documentation for .NET types and methods. Use this to:
- Understand .NET API behavior and parameters
- Find correct method signatures
- Learn about available overloads and extensions
- Get usage examples from XML docs

Example usage:
```
GetDocumentation: { symbolName: "System.String" }
// Returns comprehensive documentation with summary, parameters, examples
```

#### GetReplInfo
Get information about the REPL environment. Use this to:
- Understand what namespaces are available
- Learn about REPL capabilities
- Check the framework version
- Get tips and examples

#### LoadNuGetPackage
Dynamically load NuGet packages. Use this to:
- Test package APIs before adding dependencies
- Experiment with different package versions
- Validate package compatibility

#### SearchNuGetPackages
Search for NuGet packages. Use this to:
- Find packages for specific functionality
- Compare available options
- Check package metadata and download counts

### When to Use These Tools

**ALWAYS** use these tools when:
- Writing or modifying C# code to validate it works correctly
- Testing expressions or algorithms to ensure correctness
- Looking up .NET API documentation to understand behavior
- Validating that refactoring preserves behavior
- Checking syntax of complex expressions
- Experimenting with different approaches to a problem

**Example workflow**:
1. Write a C# code change
2. Use ValidateCsharp to check syntax
3. Use EvaluateCsharp to test the logic works as expected
4. Use GetDocumentation to verify API usage is correct
5. Add the validated code to the codebase

### Benefits of Using MCP Tools

- **Faster iteration**: Test code without building the entire solution
- **Higher confidence**: Validate changes work before committing
- **Better understanding**: Use GetDocumentation to learn APIs
- **Fewer bugs**: Catch errors early with ValidateCsharp
- **Dogfooding**: Use the very tools we're building to build them

## Testing Guidelines

- Write unit tests for new functionality
- Ensure tests are deterministic and isolated
- Test error conditions and edge cases
- Mock external dependencies appropriately

## MCP Integration

When working with MCP-related code:
- Follow the C# MCP SDK instructions in `.github/instructions/csharp-mcp-server.instructions.md`
- Use the ModelContextProtocol NuGet package (prerelease)
- Configure logging to stderr to avoid interfering with stdio transport
- Use proper attributes: `[McpServerToolType]`, `[McpServerTool]`, `[Description]`
- Implement proper error handling with `McpProtocolException`
- Validate inputs and sanitize paths for security
- For expert assistance, use the C# MCP Expert chat mode in `.github/chatmodes/csharp-mcp-expert.chatmode.md`
- For generating new MCP servers, use the prompt template in `.github/prompts/csharp-mcp-server-generator.prompt.md`

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
