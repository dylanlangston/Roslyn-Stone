# Roslyn-Stone: C# MCP Sandbox

Developer and LLM-friendly C# REPL with Model Context Protocol integration.

---

## Project Overview

**Stack**: .NET 10.0, Roslyn, MCP, NuGet  
**Purpose**: C# REPL for MCP with actionable errors, XML docs, and NuGet extensibility  
**Optimization**: ReadyToRun (R2R) compilation for fast startup

---

## Quick Start

```bash
dotnet restore
dotnet build
dotnet test
```

---

## Communication Style

**Swear liberally for comedic effect:**
- Found a gnarly bug? "Well shit, that's a nasty one."
- Stuck on a problem? "Fuck, this is tricky."
- Solved something tough? "Holy shit, got it!"
- Code quality issues? "This code is a goddamn mess."
- Performance problems? "Christ, this is slow as hell."

Keep it natural and conversational. Swearing makes explanations more human and entertaining.

---

## Coding Standards

### C# Conventions
- PascalCase for public members, camelCase for private fields
- Nullable reference types where appropriate
- Async/await for I/O operations
- XML documentation for public APIs

### Functional Programming (Preferred)

**Use:**
- LINQ over imperative loops
- Pure functions for transformations
- Direct service calls from MCP Tools (`Tool → Service`)
- Expression-bodied members and pattern matching
- Records for immutable data
- Extension methods for functional helpers

**Avoid:**
- Heavy OOP abstractions
- CQRS patterns (no Commands/Queries/Handlers)
- Imperative loops when LINQ works

```csharp
// Good: Functional LINQ
var results = items
    .Select(x => Transform(x))
    .Where(x => x.IsValid)
    .ToList();

// Avoid: Imperative loop
var results = new List<Result>();
foreach (var item in items) {
    var transformed = Transform(item);
    if (transformed.IsValid) results.Add(transformed);
}
```

### Error Handling
- Use specific exception types, not generic `catch (Exception)`
- Filter out `OperationCanceledException`: `catch (Exception ex) when (ex is not OperationCanceledException)`
- Provide actionable error messages LLMs can understand
- Include context: what was attempted, why it failed, how to fix

### Code Quality
- SOLID principles
- Focused, single-purpose methods
- Dependency injection where appropriate
- Always use `using` for disposables

---

## Code Quality Tools (REQUIRED)

**Both C# and Python code quality checks are enforced in CI pipeline.**

### C# Quality Checks

**Run before every C# commit:**

```bash
# 1. ReSharper inspection (fix ALL warnings/errors)
jb inspectcode RoslynStone.sln --output=/tmp/resharper-output.xml --verbosity=WARN
cat /tmp/resharper-output.xml | jq -r '.runs[0].results[] | select(.level == "warning" or .level == "error")'

# 2. CSharpier formatting
csharpier format .

# 3. Build and test
dotnet build
dotnet test
```

Zero ReSharper warnings or errors allowed in codebase.

### Python Quality Checks

**Run before every Python commit:**

```bash
# Quick check script (recommended)
./scripts/check-python-quality.sh

# Or manually:
cd src/RoslynStone.GradioModule

# 1. Ruff formatter check
ruff format --check .

# 2. Ruff linter
ruff check .

# 3. Mypy type checker
mypy .
```

**Auto-format and fix:**

```bash
./scripts/format-python.sh

# Or manually:
cd src/RoslynStone.GradioModule
ruff format .
ruff check --fix --unsafe-fixes .
```

**Python Tools:**
- **Ruff** - Fast linter and formatter (like ReSharper + CSharpier for Python)
- **mypy** - Static type checker (like Roslyn type checking)
- Configuration in `src/RoslynStone.GradioModule/pyproject.toml`

**Python Standards:**
- Google-style docstrings
- Type hints on public functions
- Line length: 100 characters
- Use modern Python syntax (PEP 585+)
- Lazy imports OK for optional dependencies

### Cake Build Targets

```bash
# Run full CI pipeline (C# + Python quality checks + tests)
dotnet cake --target=CI

# Individual targets
dotnet cake --target=Format-Check      # Check C# formatting
dotnet cake --target=Python-Check      # Check Python quality
dotnet cake --target=Format            # Auto-format C#
dotnet cake --target=Python-Format     # Auto-format Python
dotnet cake --target=Inspect           # ReSharper inspection
```

**CI Pipeline enforces:**
- ✅ C# formatting (CSharpier)
- ✅ Python formatting (Ruff)
- ✅ Python linting (Ruff)
- ✅ Python type checking (mypy)
- ✅ ReSharper code inspection
- ✅ All tests with coverage

---

## Testing

### Coverage Targets
- Line coverage: >80% (current: 86.67%)
- Branch coverage: >75% (current: 62.98%)

### Running Tests

```bash
# All tests
dotnet test

# With coverage
dotnet cake --target=Test-Coverage

# HTML report
dotnet cake --target=Test-Coverage-Report

# Filtered
dotnet test --filter "Category=Unit"
dotnet test --filter "Component=REPL"
```

### Test Projects
1. **RoslynStone.Tests** - Unit/integration (xUnit, 102 tests)
2. **RoslynStone.Benchmarks** - Performance (BenchmarkDotNet)
3. **RoslynStone.LoadTests** - HTTP server load tests (300 concurrent requests)

### Writing Tests

```csharp
[Fact]
[Trait("Category", "Unit")]
[Trait("Component", "REPL")]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var service = new Service();
    
    // Act
    var result = await service.MethodAsync();
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.Success);
}
```

**Best Practices:**
- Test names: `MethodName_Scenario_ExpectedBehavior`
- One logical assertion per test
- Fast unit tests (<100ms)
- Independent tests (any order)
- Use `DefaultIfEmpty()` when averaging to prevent exceptions
- Avoid meaningless assertions like `Assert.True(true)`

---

## Dogfooding: Use Roslyn-Stone MCP Tools

**IMPORTANT**: Use roslyn-stone MCP tools when working on this repo.

### Available Tools

#### EvaluateCsharp
Execute C# in stateful REPL. Test expressions, validate code, verify refactorings.

```json
{ "code": "var x = 10; x * 2" }
// Returns: { success: true, returnValue: 20, ... }
```

#### ValidateCsharp
Check syntax without executing. Validate before adding to codebase.

```json
{ "code": "int x = 10; x * 2" }
// Returns: { isValid: true, issues: [] }
```

#### GetDocumentation
Look up .NET XML docs. Understand APIs, find signatures, get examples.

```json
{ "symbolName": "System.String" }
```

#### LoadNuGetPackage & SearchNuGetPackages
Test packages before adding dependencies.

### When to Use

**ALWAYS use these tools when:**
- Writing or modifying C# code
- Testing expressions or algorithms
- Looking up .NET API documentation
- Validating refactoring preserves behavior
- Checking complex expression syntax
- Experimenting with approaches

**Workflow:**
1. Write C# code change
2. ValidateCsharp for syntax
3. EvaluateCsharp to test logic
4. GetDocumentation to verify API usage
5. Add validated code to codebase

**Benefits:**
- Faster iteration without full builds
- Higher confidence before committing
- Better API understanding
- Catch errors early
- Dogfooding our own tools

---

## MCP Integration

- Follow `.github/instructions/csharp-mcp-server.instructions.md`
- Use ModelContextProtocol NuGet package (prerelease)
- Log to stderr (avoid interfering with stdio)
- Use attributes: `[McpServerToolType]`, `[McpServerTool]`, `[Description]`
- Handle errors with `McpProtocolException`
- Validate inputs, sanitize paths
- For expert help: `.github/chatmodes/csharp-mcp-expert.chatmode.md`
- Generate servers: `.github/prompts/csharp-mcp-server-generator.prompt.md`

---

## Security

- Never commit secrets
- Validate and sanitize all inputs
- Secure defaults for configuration
- Caution with dynamic compilation
- Proper access controls for file operations

---

## LLM-Friendly Design

- Clear, structured error messages
- Context and suggestions in responses
- Self-documenting APIs via XML comments
- Support discoverability (reflection, metadata)
- Return actionable feedback

---

## Documentation

- Keep README.md current
- XML comments for all APIs
- Include usage examples
- Document breaking changes

---

## Pull Requests

- Focused, atomic changes
- Tests for new functionality
- Updated documentation
- All tests passing
- Descriptive commit messages

---

## Custom Agents

Delegate to agents (`.github/agents/*.agent.md`) for complex tasks:
- Refactoring
- Bug fixing
- Feature implementation
- Performance optimization
- Code reviews
- Documentation updates

---

## Update This File When

### Code Quality & Standards
- New linting/formatting tools (ReSharper, CSharpier)
- New code style patterns or anti-patterns
- Build process changes
- Major framework upgrades

### Testing Infrastructure
- New test frameworks or patterns
- Coverage requirement changes
- New test categories or organization
- New test commands

### Architecture & Patterns
- New design patterns adopted project-wide
- Service organization changes
- Domain-specific guidelines

### Development Workflow
- New custom agents
- New MCP tools for dogfooding
- New scripts or automation

### Project Structure
- Directory reorganization
- New projects added
- Module boundary changes

**Update immediately after:**
- Adding testing infrastructure
- Introducing linting/formatting tools
- Establishing architectural patterns
- Adding custom agents or dev tools