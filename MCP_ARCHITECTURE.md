# MCP Architecture

This document describes the Model Context Protocol (MCP) architecture implementation in Roslyn-Stone.

## Overview

Roslyn-Stone implements MCP following best practices by properly distinguishing between:
- **Tools**: Active functions that perform operations
- **Resources**: Passive data sources providing read-only access
- **Prompts**: Optimized templates guiding LLM usage

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

Execute C# code in a REPL session.

**Parameters**:
- `code`: C# code to execute
- `contextId` (optional): Session context ID

**Returns**: `{ success, returnValue, output, errors, warnings, executionTime, contextId }`

**Workflow**:
1. First call without contextId → creates session, returns contextId
2. Subsequent calls with contextId → continue in same session

### ValidateCsharp

Validate C# syntax without execution.

**Parameters**:
- `code`: C# code to validate
- `contextId` (optional): Session context for context-aware validation

**Returns**: `{ isValid, issues: [{ code, message, severity, line, column }] }`

**Modes**:
- Context-free: Syntax-only validation
- Context-aware: Validates against session variables/types

### ResetRepl

Reset REPL sessions.

**Parameters**:
- `contextId` (optional): Specific session to reset

**Behavior**:
- With contextId: Reset specific session
- Without contextId: Reset all sessions

**Returns**: `{ success, message, sessionsCleared }`

### LoadNuGetPackage

Load a NuGet package into the REPL.

**Parameters**:
- `packageName`: Package ID
- `version` (optional): Specific version (omit for latest stable)

**Behavior**: Package loaded into session, persists until ResetRepl

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

Prompts guide LLMs in using Roslyn-Stone effectively. All prompts are token-optimized and reference Resources.

### Available Prompts

1. **QuickStartRepl** (150 tokens): Bare minimum quick start
2. **GetStartedWithCsharpRepl** (600 tokens): Comprehensive introduction
3. **DebugCompilationErrors** (250 tokens): Error handling workflow
4. **ReplBestPractices** (350 tokens): Best practices and patterns
5. **WorkingWithPackages** (250 tokens): NuGet essentials
6. **PackageIntegrationGuide** (450 tokens): Detailed package workflow

### Prompt Design Principles

- **Concise**: 60-70% smaller than original (4500→1850 tokens total)
- **Resource-driven**: Reference doc://, nuget://, repl:// instead of embedding examples
- **Context-aware**: Document optional contextId parameters
- **Workflow-focused**: Resource query → Tool usage → Iteration
- **Bullet points**: Replace verbose paragraphs

## Workflow Patterns

### Iterative Development

```
1. Access doc://System.Linq.Enumerable.Select (learn API)
2. ValidateCsharp(code, contextId) (check syntax)
3. EvaluateCsharp(code, contextId) (execute)
4. Access repl://sessions/{contextId}/state (check session)
```

### Package Integration

```
1. Access nuget://search?q=json (explore packages)
2. Access nuget://packages/Newtonsoft.Json/readme (read docs)
3. LoadNuGetPackage("Newtonsoft.Json") (load)
4. EvaluateCsharp(code with using directive, contextId) (use)
```

### Session Management

```
1. EvaluateCsharp(code) → get contextId
2. EvaluateCsharp(more code, contextId) → continue session
3. Access repl://sessions/{contextId}/state → check metadata
4. ResetRepl(contextId) → clean up
```

## Design Decisions

### Why Resources?

- **Efficiency**: Read-only data access without execution overhead
- **Discoverability**: LLMs can explore available data
- **Caching**: Resources can be cached by clients
- **Separation**: Clear distinction between data (Resources) and operations (Tools)

### Why Context Management?

- **Statefulness**: Enable multi-step development workflows
- **Isolation**: Multiple concurrent sessions without interference
- **Flexibility**: Support both single-shot and session-based execution
- **Lifecycle**: Automatic cleanup prevents resource leaks

### Why Optimize Prompts?

- **Token efficiency**: Reduce LLM context usage by 59%
- **Clarity**: Concise prompts are easier to understand
- **Maintainability**: Smaller prompts are easier to update
- **Resource-driven**: Leverage Resources for examples instead of embedding

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
