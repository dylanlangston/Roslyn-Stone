# MCP Architecture

This document describes the Model Context Protocol (MCP) architecture implementation in Roslyn-Stone.

## Overview

Roslyn-Stone implements MCP to help LLMs create single-file C# utility programs (file-based apps). It properly distinguishes between:
- **Tools**: Active functions that execute, validate, and build utility programs
- **Resources**: Passive data sources providing read-only access to documentation and package info
- **Prompts**: Optimized templates guiding LLMs to create runnable .cs files

## Design Philosophy

The goal is to enable LLMs to create complete, runnable single-file C# programs using top-level statements. This aligns with .NET's file-based app feature (`dotnet run app.cs`), which eliminates the need for project files and boilerplate code.

**Target output:** Complete .cs files that can be run directly, perfect for:
- Command-line utilities
- Data processing scripts
- Automation tools
- Quick C# programs without project scaffolding

## Resources (Passive Data Access)

Resources provide read-only, URI-based access to data. They enable efficient discovery and querying without execution.

### DocumentationResource

Provides access to .NET XML documentation.

**URI Pattern**: `doc://{symbolName}`

**Examples**:
```
doc://System.String
doc://System.Linq.Enumerable.Select
doc://System.Collections.Generic.List`1
```

**Response**: Structured documentation with summary, parameters, return values, examples, and related types.

### NuGetSearchResource

Searchable catalog of NuGet packages.

**URI Pattern**: `nuget://search?q={query}&skip={skip}&take={take}`

**Examples**:
```
nuget://search?q=json
nuget://search?q=http%20client&skip=0&take=10
```

**Response**: Package list with ID, description, version, download count, tags.

### NuGetPackageResource

Package-specific information.

**URI Patterns**:
- Versions: `nuget://packages/{id}/versions`
- README: `nuget://packages/{id}/readme?version={version}`

**Examples**:
```
nuget://packages/Newtonsoft.Json/versions
nuget://packages/Newtonsoft.Json/readme
nuget://packages/Newtonsoft.Json/readme?version=13.0.3
```

**Response**: Version list or README content in Markdown.

### ReplStateResource

REPL environment and session state.

**URI Patterns**:
- General: `repl://state` or `repl://info`
- Session-specific: `repl://sessions/{contextId}/state`

**Examples**:
```
repl://state
repl://sessions/abc-123-def-456/state
```

**Response**: Framework version, capabilities, active session count, session metadata.

## Tools (Active Operations)

Tools perform operations and can modify state. All REPL tools support context management.

### Context-Aware Design

All tools accept an optional `contextId` parameter:
- **Without contextId**: Single-shot execution (context created, can be reused)
- **With contextId**: Session-based execution (state persists)

### EvaluateCsharp

Execute C# code to create and test single-file utility programs.

**Parameters**:
- `code`: C# code to execute (use top-level statements for file-based apps)
- `contextId` (optional): Session context ID for iterative development
- `createContext` (optional): Create persistent context for multi-step building

**Returns**: `{ success, returnValue, output, errors, warnings, executionTime, contextId }`

**Use Cases**:
- Test complete utility programs
- Iteratively build single-file apps
- Validate program logic before finalizing

### ValidateCsharp

Validate C# syntax and semantics for single-file utility programs.

**Parameters**:
- `code`: C# code to validate (use top-level statements)
- `contextId` (optional): Session context for context-aware validation

**Returns**: `{ isValid, issues: [{ code, message, severity, line, column }] }`

**Use Cases**:
- Check utility program syntax before execution
- Validate complete .cs files
- Context-aware validation against session variables

### ResetRepl

Reset REPL sessions.

**Parameters**:
- `contextId` (optional): Specific session to reset

**Behavior**:
- With contextId: Reset specific session
- Without contextId: Reset all sessions

**Returns**: `{ success, message, sessionsCleared }`

### LoadNuGetPackage

Load a NuGet package for use in utility programs.

**Parameters**:
- `packageName`: Package ID
- `version` (optional): Specific version (omit for latest stable)

**Use Cases**: Add functionality to single-file utility programs (JSON processing, HTTP clients, etc.)

### GetReplInfo

Get current execution environment information and capabilities.

**Parameters**:
- `contextId` (optional): Session context for session-specific information

**Returns**: `{ frameworkVersion, language, state, activeSessionCount, contextId, isSessionSpecific, defaultImports, capabilities, tips, examples, sessionMetadata }`

**Use Cases**:
- Understand environment capabilities
- Check active session count
- Get tips for creating utility programs
- Access example code patterns

### SearchNuGetPackages

Search for NuGet packages (Tool alternative to `nuget://search` resource).

**Parameters**:
- `query`: Search query
- `skip` (optional): Pagination offset (default: 0)
- `take` (optional): Results to return (default: 20, max: 100)

**Returns**: `{ packages, totalCount, query, skip, take }`

**Use Cases**: For clients that don't support MCP resources

### GetNuGetPackageVersions

Get all versions of a NuGet package (Tool alternative to `nuget://packages/{id}/versions` resource).

**Parameters**:
- `packageId`: Package ID to query

**Returns**: `{ found, packageId, versions, totalCount }`

**Use Cases**: For clients that don't support MCP resources

### GetNuGetPackageReadme

Get README content for a NuGet package (Tool alternative to `nuget://packages/{id}/readme` resource).

**Parameters**:
- `packageId`: Package ID
- `version` (optional): Specific version (omit for latest)

**Returns**: `{ found, packageId, version, content }`

**Use Cases**: For clients that don't support MCP resources

### GetDocumentation

Get XML documentation for .NET types/methods (Tool alternative to `doc://` resource).

**Parameters**:
- `symbolName`: Fully qualified type or method name
- `packageId` (optional): NuGet package ID for package-specific types

**Returns**: `{ found, symbolName, summary, remarks, parameters, returns, exceptions, example }`

**Use Cases**: For clients that don't support MCP resources

**Examples**:
- GetDocumentation("System.String")
- GetDocumentation("JsonConvert", "Newtonsoft.Json")

## Context Management

### IReplContextManager

Interface for managing REPL session lifecycle.

**Key Methods**:
- `CreateContext()`: Create new session, return GUID
- `ContextExists(contextId)`: Check if session exists
- `GetContextState(contextId)`: Retrieve script state
- `UpdateContextState(contextId, state)`: Update script state
- `RemoveContext(contextId)`: Delete session
- `CleanupExpiredContexts()`: Remove old sessions

### ReplContextManager

Thread-safe implementation using `ConcurrentDictionary`.

**Features**:
- Configurable timeout (default: 30 minutes)
- Automatic cleanup of expired sessions
- Metadata tracking (creation time, last access, execution count)
- Thread-safe concurrent operations

### Context Metadata

Tracks session information:
- `ContextId`: Unique identifier (GUID)
- `CreatedAt`: Session creation timestamp
- `LastAccessedAt`: Last activity timestamp
- `ExecutionCount`: Number of executions
- `IsInitialized`: Whether code has been executed

## Prompts (Optimized Templates)

Prompts guide LLMs in creating single-file C# utility programs. All prompts focus on file-based app patterns.

### Available Prompts

1. **QuickStartRepl** (150 tokens): Quick start for creating single-file utilities
2. **GetStartedWithCsharpRepl** (800 tokens): Comprehensive guide to file-based C# apps with complete examples
3. **DebugCompilationErrors** (250 tokens): Error handling workflow
4. **ReplBestPractices** (600 tokens): Best practices for creating utility programs with complete examples
5. **WorkingWithPackages** (400 tokens): Using NuGet packages in utility programs
6. **PackageIntegrationGuide** (700 tokens): Detailed package integration with 4 complete utility examples

### Prompt Design Principles

- **File-based focus**: Guide LLMs to create complete, runnable .cs files
- **Top-level statements**: Emphasize simple, boilerplate-free code
- **Complete examples**: Show full utility programs, not just snippets
- **Resource-driven**: Reference doc://, nuget:// for API lookup
- **Context-aware**: Document optional contextId for iterative development
- **Workflow-focused**: Resource query → Build → Test → Refine
- **Practical patterns**: Real-world utility examples (file processing, HTTP clients, etc.)

## Workflow Patterns

### Creating a Simple Utility

```
1. Access doc://System.IO.File (learn API)
2. ValidateCsharp(code) (check syntax)
3. EvaluateCsharp(code) (test program)
4. → Complete utility.cs file
```

### Building with Packages

```
1. Access nuget://search?q=json (find package)
2. Access nuget://packages/Newtonsoft.Json/readme (read docs)
3. LoadNuGetPackage("Newtonsoft.Json") (load)
4. EvaluateCsharp(complete utility code) (build and test)
5. → Complete json-processor.cs file with package imports
```

### Iterative Development

```
1. EvaluateCsharp(initial code, createContext: true) → get contextId
2. ValidateCsharp(next code, contextId) → check additions
3. EvaluateCsharp(next code, contextId) → test incremental changes
4. Repeat steps 2-3 as needed
5. → Final complete utility program
```

## Design Decisions

### Why Focus on File-Based Apps?

- **Simplicity**: No project files, build configuration, or boilerplate code
- **Quick utilities**: Perfect for creating small, focused programs
- **LLM-friendly**: Clear goal (complete .cs file) vs. open-ended REPL experimentation
- **Real-world use**: Aligns with .NET's `dotnet run app.cs` feature
- **Completeness**: Guides toward finished programs, not code snippets

### Why Resources?

- **Efficiency**: Read-only data access without execution overhead
- **Discoverability**: LLMs can explore available data
- **Caching**: Resources can be cached by clients
- **Separation**: Clear distinction between data (Resources) and operations (Tools)

### Why Context Management?

- **Iterative development**: Build utility programs step by step
- **Isolation**: Multiple concurrent sessions without interference
- **Flexibility**: Support both single-shot and iterative development
- **Lifecycle**: Automatic cleanup prevents resource leaks

### Why Optimize Prompts?

- **Clear guidance**: Help LLMs create complete, runnable programs
- **Token efficiency**: Focus on essential patterns and examples
- **Practical examples**: Show real utility programs, not toy snippets
- **Resource-driven**: Leverage Resources for API docs instead of embedding
- **Goal-oriented**: Guide toward finished .cs files

## Performance Considerations

### Resource Caching

- Documentation lookups cached by symbol name
- NuGet queries cached by search terms
- Package metadata cached by ID

### Context Cleanup

- Automatic background cleanup every X minutes
- Configurable timeout (default: 30 minutes)
- Manual cleanup via ResetRepl

### Concurrent Sessions

- Thread-safe ConcurrentDictionary
- No lock contention for independent sessions
- Isolation prevents cross-session interference

## Testing

### Test Coverage

- **Resources**: 12 tests covering URI parsing, error handling, response structure
- **Context Management**: 19 tests covering lifecycle, thread safety, cleanup
- **Tools**: 11 integration tests updated for context-aware API
- **Total**: 580+ new assertions, 134 tests passing

### Test Categories

- Unit tests: Components in isolation
- Integration tests: Tool → Service → Context interactions
- Resource tests: URI handling and response validation
- Thread safety tests: Concurrent context operations

## Future Enhancements

### Potential Additions

1. **Context persistence**: Save/restore sessions across server restarts
2. **Context limits**: Enforce maximum context count/size
3. **Context listing resource**: `repl://sessions` to enumerate all
4. **Package documentation resource**: Loaded package type docs
5. **Execution history resource**: Per-session execution log

### Optimization Opportunities

1. Lazy loading for Resources
2. Response streaming for large results
3. Incremental compilation caching
4. NuGet package preloading

## References

- Model Context Protocol: https://modelcontextprotocol.io
- MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- Roslyn Scripting: https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples
