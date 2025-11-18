using System.ComponentModel;
using ModelContextProtocol.Server;

namespace RoslynStone.Infrastructure.Tools;

/// <summary>
/// MCP prompts to guide LLMs in using Roslyn-Stone effectively
/// </summary>
[McpServerPromptType]
public class GuidancePrompts
{
    /// <summary>
    /// Quick start guide for getting started immediately
    /// </summary>
    [McpServerPrompt]
    [Description("Get the bare minimum to start using the C# REPL right now")]
    public static Task<string> QuickStartRepl()
    {
        return Task.FromResult(
            @"# Quick Start: C# REPL

**Execute C# code:**
```
EvaluateCsharp(code: ""2 + 2"")
→ { success: true, returnValue: 4 }
```

**Stateful execution** - variables persist:
```
EvaluateCsharp(code: ""var x = 10;"")
EvaluateCsharp(code: ""x * 2"")
→ 20
```

**Context-aware sessions** - optional contextId:
```
// Single-shot (no state)
EvaluateCsharp(code: ""1 + 1"")

// Session-based (preserves state)
EvaluateCsharp(code: ""var x = 5;"", contextId: ""session1"")
EvaluateCsharp(code: ""x * 2"", contextId: ""session1"")
```

**Key Tools:**
- `EvaluateCsharp` - Execute code
- `ValidateCsharp` - Check syntax
- `GetDocumentation` - Look up .NET APIs (doc://System.String)
- `ResetRepl` - Clear state

**Resources:**
- `doc://{symbol}` - API documentation
- `repl://state` - REPL environment info
- `repl://sessions/{contextId}/state` - Session details

**Workflow:**
1. Query resource → 2. Execute code → 3. Iterate"
        );
    }

    /// <summary>
    /// Comprehensive introduction to REPL capabilities
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Get a comprehensive introduction to Roslyn-Stone's capabilities and how to use the C# REPL effectively"
    )]
    public static Task<string> GetStartedWithCsharpRepl()
    {
        return Task.FromResult(
            @"# Getting Started with Roslyn-Stone C# REPL

## Core Features
- **Execute C# code** - Full .NET 10 support with async/await
- **Stateful sessions** - Variables persist (single-shot or session-based)
- **Actionable errors** - Line numbers, suggestions, error codes
- **API documentation** - XML docs via `doc://{symbol}` resources
- **NuGet packages** - Dynamic package loading

## Context-Aware Execution

### Single-Shot (No Context)
```
EvaluateCsharp(code: ""Math.Sqrt(16)"")
```
Variables don't persist between calls.

### Session-Based (With Context)
```
EvaluateCsharp(code: ""var x = 10;"", contextId: ""my-session"")
EvaluateCsharp(code: ""x * 2"", contextId: ""my-session"")
```
State persists within the same contextId.

## Essential Tools

**Code Execution:**
- `EvaluateCsharp(code, contextId?)` - Run C# code
- `ValidateCsharp(code, contextId?)` - Check syntax without execution

**Documentation:**
- `GetDocumentation(symbolName)` - Get XML docs
- Resource: `doc://System.String`, `doc://System.Linq.Enumerable.Select`

**Packages:**
- `SearchNuGetPackages(query)` - Find packages
- `LoadNuGetPackage(packageName, version?)` - Add packages
- `GetNuGetPackageVersions(packageId)` - List versions
- Resources: `nuget://search?q=json`, `nuget://packages/{id}/readme`

**State Management:**
- `GetReplInfo(contextId?)` - Environment info
- `ResetRepl(contextId?)` - Clear state (specific session or all)
- Resources: `repl://state`, `repl://sessions/{contextId}/state`

## Workflow Pattern

**Query → Execute → Iterate:**
1. Look up API: `doc://System.String.Split`
2. Validate syntax: `ValidateCsharp(code: ""..."")`
3. Execute code: `EvaluateCsharp(code: ""..."")`
4. Check result and refine

## Key Capabilities

**Console Output:**
```
EvaluateCsharp(code: ""Console.WriteLine(\""Debug\""); return 42;"")
→ { returnValue: 42, output: ""Debug\n"" }
```

**Error Feedback:**
```
ValidateCsharp(code: ""string x = 123;"")
→ { isValid: false, issues: [{ code: ""CS0029"", line: 1, message: ""..."" }] }
```

**Async Support:**
```
EvaluateCsharp(code: ""await Task.Delay(100); return \""Done\"";"")
```

## Resource References

Instead of guessing, query resources:
- **API docs:** `doc://System.Collections.Generic.List\`1`
- **Package search:** `nuget://search?q=json+serialization`
- **Package info:** `nuget://packages/Newtonsoft.Json/versions`
- **REPL state:** `repl://state` or `repl://sessions/my-session/state`

Use GetDocumentation, SearchNuGetPackages, GetReplInfo to access these resources."
        );
    }

    /// <summary>
    /// Guide for debugging compilation errors effectively
    /// </summary>
    [McpServerPrompt]
    [Description("Learn how to use ValidateCsharp effectively to debug compilation errors")]
    public static Task<string> DebugCompilationErrors()
    {
        return Task.FromResult(
            @"# Debugging Compilation Errors

## Validate Before Execute

**Always validate complex code first:**
```
ValidateCsharp(code: ""string x = 123;"")
→ { isValid: false, issues: [{ code: ""CS0029"", line: 1, message: ""Cannot convert..."" }] }
```

## Context-Aware Validation

**Without context** - syntax check only:
```
ValidateCsharp(code: ""x * 2"")
→ Error: 'x' does not exist
```

**With context** - validates against session state:
```
EvaluateCsharp(code: ""var x = 10;"", contextId: ""session1"")
ValidateCsharp(code: ""x * 2"", contextId: ""session1"")
→ { isValid: true }
```

## Understanding Error Messages

**Error structure:**
- `code` - CS#### error code (look up online if needed)
- `line/column` - Exact location
- `message` - What's wrong
- `severity` - ""Error"" or ""Warning""

## Common Patterns

**Missing namespace:**
```
ValidateCsharp(code: ""var list = new List<int>();"")
→ Fix: Add 'using System.Collections.Generic;'
```

**Type mismatch:**
```
ValidateCsharp(code: ""int x = \""hello\"";"")
→ Fix: Use int.Parse(""123"") or proper type
```

**Undefined variable (context-free):**
```
ValidateCsharp(code: ""y + 10"")
→ Fix: Define 'y' first or use contextId with existing session
```

## Workflow

1. Write code
2. Validate: `ValidateCsharp(code, contextId?)`
3. Read errors and fix
4. Re-validate until `isValid: true`
5. Execute: `EvaluateCsharp(code, contextId?)`

**Use resources for help:**
- `doc://{symbol}` - Check API usage via GetDocumentation
- `repl://sessions/{contextId}/state` - Check session variables"
        );
    }

    /// <summary>
    /// Best practices and patterns for REPL usage
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Learn best practices for iterative C# code development using the REPL for experimentation and rapid prototyping"
    )]
    public static Task<string> ReplBestPractices()
    {
        return Task.FromResult(
            @"# REPL Best Practices & Patterns

## Session Management

**Single-shot execution** (no state):
```
EvaluateCsharp(code: ""DateTime.Now"")
```
Good for: One-off calculations, testing snippets

**Session-based execution** (stateful):
```
EvaluateCsharp(code: ""var data = new[] { 1, 2, 3 };"", contextId: ""work"")
EvaluateCsharp(code: ""data.Sum()"", contextId: ""work"")
```
Good for: Building complex solutions, multi-step workflows

**Multiple sessions** (isolated contexts):
```
EvaluateCsharp(code: ""var x = 1;"", contextId: ""session-a"")
EvaluateCsharp(code: ""var x = 2;"", contextId: ""session-b"")
```
Sessions don't interfere with each other.

## Incremental Development

**Build complexity gradually:**
```
// 1. Define structure
EvaluateCsharp(code: ""class Person { public string Name; }"", contextId: ""dev"")

// 2. Create instance
EvaluateCsharp(code: ""var p = new Person { Name = \""Alice\"" };"", contextId: ""dev"")

// 3. Test operations
EvaluateCsharp(code: ""p.Name.ToUpper()"", contextId: ""dev"")
```

## Debugging with Console Output

**Separate return value from debug output:**
```
EvaluateCsharp(code: @""
    var items = new[] { 1, 2, 3, 4, 5 };
    Console.WriteLine($\""Processing {items.Length} items\"");
    
    var result = items.Where(x => x > 2).Sum();
    Console.WriteLine($\""Sum: {result}\"");
    
    return result;
"")
→ { returnValue: 12, output: ""Processing 5 items\nSum: 12\n"" }
```

## Validation Strategy

**Validate before expensive operations:**
```
// 1. Check syntax first (fast, safe)
ValidateCsharp(code: ""...<complex code>..."", contextId?)

// 2. Only execute if valid
EvaluateCsharp(code: ""...<complex code>..."", contextId?)
```

## Resource-Driven Workflow

**Query before coding:**
```
// 1. Look up API documentation
GetDocumentation(symbolName: ""System.String.Split"")
→ or resource: doc://System.String.Split

// 2. Write code based on docs
EvaluateCsharp(code: ""\""a,b,c\"".Split(',')"")
```

## State Management

**Check current state:**
```
GetReplInfo(contextId: ""my-session"")
→ See variables, assemblies, session metadata
```

**Reset when needed:**
```
// Reset specific session
ResetRepl(contextId: ""session1"")

// Reset all sessions
ResetRepl()
```

**When to reset:**
- Variable name conflicts
- Starting fresh experiment
- After package version changes
- State inconsistency

## Error Handling

**Try-catch for robustness:**
```
EvaluateCsharp(code: @""
    try {
        return int.Parse(input);
    }
    catch (FormatException) {
        return -1; // Default
    }
"")
```

## Performance

**Check execution time:**
```
EvaluateCsharp(code: ""Enumerable.Range(1, 1000000).Sum()"")
→ { executionTime: ""...<time>..."" }
```

## Tips

- Use `await` for async operations (fully supported)
- Last expression is auto-returned (no need for explicit `return`)
- Console.WriteLine for debugging, return value for results
- Leverage sessions for complex, multi-step workflows
- Query `repl://state` to understand environment capabilities"
        );
    }

    /// <summary>
    /// Guide for working with NuGet packages
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Learn how to discover, load, and use NuGet packages in the C# REPL for extended functionality"
    )]
    public static Task<string> WorkingWithPackages()
    {
        return Task.FromResult(
            @"# Working with NuGet Packages

## Package Workflow

**1. Search** - Find packages:
```
SearchNuGetPackages(query: ""json serialization"", take: 5)
→ Resource: nuget://search?q=json+serialization
```

**2. Research** - Get package info:
```
GetNuGetPackageVersions(packageId: ""Newtonsoft.Json"")
→ Resource: nuget://packages/Newtonsoft.Json/versions

GetNuGetPackageReadme(packageId: ""Newtonsoft.Json"")
→ Resource: nuget://packages/Newtonsoft.Json/readme
```

**3. Load** - Add package to REPL:
```
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""13.0.3"")
// Version is optional - omit for latest stable
```

**4. Use** - Import and code:
```
EvaluateCsharp(code: @""
    using Newtonsoft.Json;
    var data = new { Name = \""Alice\"", Age = 30 };
    return JsonConvert.SerializeObject(data);
"")
```

## Complete Example

```
// Find package
SearchNuGetPackages(query: ""csv parser"", take: 3)

// Check versions (avoid prereleases)
GetNuGetPackageVersions(packageId: ""CsvHelper"")

// Read docs to understand usage
GetNuGetPackageReadme(packageId: ""CsvHelper"")

// Load stable version
LoadNuGetPackage(packageName: ""CsvHelper"", version: ""30.0.1"")

// Use it
EvaluateCsharp(code: @""
    using CsvHelper;
    using System.Globalization;
    using var writer = new StringWriter();
    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
    
    csv.WriteRecords(new[] {
        new { Id = 1, Name = \""Alice\"" },
        new { Id = 2, Name = \""Bob\"" }
    });
    
    return writer.ToString();
"")
```

## Common Packages

- **JSON**: Newtonsoft.Json (nuget://packages/Newtonsoft.Json/readme)
- **HTTP**: Flurl.Http, RestSharp
- **CSV**: CsvHelper
- **Utilities**: Humanizer, MoreLINQ
- **Fake Data**: Bogus

## Best Practices

- Choose stable versions (not prerelease)
- Check download counts (popularity = reliability)
- Read README before loading (understand API)
- Test after loading with simple example
- Dependencies auto-load (no manual intervention)

## Troubleshooting

**Package not found?**
- Verify package ID spelling via nuget://search
- Check version exists via nuget://packages/{id}/versions

**Using directive errors?**
- Must add `using` statement after loading package
- Check README for correct namespace

**Version conflict?**
- Reset and reload: `ResetRepl()` then `LoadNuGetPackage(...)`"
        );
    }

    /// <summary>
    /// Comprehensive package integration guide
    /// </summary>
    [McpServerPrompt]
    [Description("Deep dive into NuGet package integration with detailed examples")]
    public static Task<string> PackageIntegrationGuide()
    {
        return Task.FromResult(
            @"# NuGet Package Integration Guide

## Discovery Strategy

**Use resource-driven search:**
```
SearchNuGetPackages(query: ""http client"", take: 10)
→ nuget://search?q=http+client
```

**Evaluate results:**
- `downloadCount` - Popularity indicator
- `description` - Feature overview
- `latestVersion` - Current stable release

## Investigation Phase

**Check version history:**
```
GetNuGetPackageVersions(packageId: ""Flurl.Http"")
→ nuget://packages/Flurl.Http/versions
```
- Avoid deprecated versions
- Prefer stable over prerelease
- Note download counts per version

**Read documentation:**
```
GetNuGetPackageReadme(packageId: ""Flurl.Http"")
→ nuget://packages/Flurl.Http/readme
```
README typically includes:
- Installation guide
- Quick start examples
- API overview
- Migration notes

## Integration Workflow

**Load package (with or without version):**
```
// Latest stable
LoadNuGetPackage(packageName: ""Humanizer"")

// Specific version
LoadNuGetPackage(packageName: ""Humanizer"", version: ""2.14.1"")
```

**Verify loading:**
```
EvaluateCsharp(code: @""
    using Humanizer;
    return \""PascalCase\"".Humanize();
"")
→ ""Pascal case""
```

## Advanced Examples

### JSON with Newtonsoft.Json
```
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""13.0.3"")

EvaluateCsharp(code: @""
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    
    var json = \""{\\\""name\\\"":\\\""Alice\\\"",\\\""age\\\"":30}\"";
    var obj = JObject.Parse(json);
    obj[\""age\""] = 31;
    
    return obj.ToString(Formatting.Indented);
"")
```

### Fake Data with Bogus
```
LoadNuGetPackage(packageName: ""Bogus"")

EvaluateCsharp(code: @""
    using Bogus;
    
    var personFaker = new Faker<Person>()
        .RuleFor(p => p.Name, f => f.Name.FullName())
        .RuleFor(p => p.Email, f => f.Internet.Email());
    
    return personFaker.Generate(3);
"", contextId: ""demo"")
```

### HTTP with Flurl
```
LoadNuGetPackage(packageName: ""Flurl.Http"")

EvaluateCsharp(code: @""
    using Flurl.Http;
    
    // Note: Requires network access
    var result = await \""https://api.example.com/data\""
        .WithHeader(\""Accept\"", \""application/json\"")
        .GetJsonAsync<dynamic>();
    
    return result;
"")
```

## Package Categories

**Data Formats:**
- JSON: Newtonsoft.Json, System.Text.Json
- CSV: CsvHelper
- XML: System.Xml.Linq (built-in)
- YAML: YamlDotNet

**Networking:**
- HTTP: Flurl.Http, RestSharp, Refit
- WebSockets: Websocket.Client

**Utilities:**
- Strings: Humanizer
- LINQ: MoreLINQ
- Validation: FluentValidation
- Mapping: AutoMapper
- Retry: Polly

**Testing/Mocking:**
- Data: Bogus, AutoFixture
- Assertions: FluentAssertions

## Resource References

Instead of blind searching, use structured queries:
```
nuget://search?q=json              // General search
nuget://search?q=tag:json          // Tag-based
nuget://packages/{id}/versions     // Version list
nuget://packages/{id}/readme       // Documentation
```

Query with GetNuGetPackageVersions, GetNuGetPackageReadme, SearchNuGetPackages.

## State & Packages

Packages persist until `ResetRepl()`:
```
LoadNuGetPackage(packageName: ""Humanizer"")
// Available in all subsequent EvaluateCsharp calls

ResetRepl()  // Clears packages too
// Must reload packages after reset
```

## Performance Tips

- Load packages once per session
- Dependencies auto-resolve (don't load manually)
- Avoid loading multiple versions of same package (causes conflicts)"
        );
    }

}
