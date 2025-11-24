using System.ComponentModel;
using ModelContextProtocol.Server;

namespace RoslynStone.Infrastructure.Tools;

/// <summary>
/// MCP prompts to guide LLMs in using Roslyn-Stone for creating file-based C# apps
/// </summary>
[McpServerPromptType]
public class GuidancePrompts
{
    /// <summary>
    /// Quick start guide for creating single-file C# utility programs
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Get the bare minimum to start creating single-file C# utility programs right now"
    )]
    public static Task<string> QuickStart()
    {
        return Task.FromResult(
            @"# Quick Start: Single-File C# Utility Programs

**Create a simple utility:**
```
EvaluateCsharp(code: ""Console.WriteLine(\""Hello, World!\"");"")
→ { success: true, output: ""Hello, World!\n"" }
```

**File-based app pattern** - top-level statements:
```csharp
// hello.cs - Simple utility with no dependencies
Console.WriteLine(""Starting utility..."");
var files = Directory.GetFiles(""."");
foreach (var file in files)
    Console.WriteLine(file);
// Run with: dotnet run hello.cs
```

**File-based app with NuGet package** - use `#:package` directive:
```csharp
// text-humanizer.cs - Self-contained utility with package
#:package Humanizer@2.14.1

using Humanizer;

var text = args.Length > 0 ? args[0] : ""HelloWorld"";
Console.WriteLine($""Original: {text}"");
Console.WriteLine($""Humanized: {text.Humanize()}"");
// Run with: dotnet run text-humanizer.cs PascalCaseText
```

**Testing with packages** - use nugetPackages parameter:
```
EvaluateCsharp(
    code: ""using Humanizer; \""test\"".Humanize()"",
    nugetPackages: [{packageName: ""Humanizer"", version: ""2.14.1""}]
)
→ Tests package functionality inline
```

**Key Tools:**
- `EvaluateCsharp` - Execute and test utility code (supports nugetPackages parameter)
- `ValidateCsharp` - Check syntax before running
- Resource: `doc://{symbol}` - Look up .NET APIs (e.g., doc://System.IO.File)

**Resources:**
- `doc://{symbol}` - API documentation
- `nuget://search?q={query}` - Find packages

**New .NET 10 Syntax:**
- `#:package <PackageName>@<Version>` - Include NuGet packages directly in .cs files (no .csproj needed)
- `#:sdk <SdkName>` - Change project SDK (e.g., Microsoft.NET.Sdk.Web for web apps)

**Workflow:**
1. Look up APIs → 2. Test with nugetPackages → 3. Finalize with `#:package` directive → 4. Complete self-contained .cs file"
        );
    }

    /// <summary>
    /// Comprehensive guide combining getting started, best practices, and development patterns
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Comprehensive guide for creating single-file C# utility programs with best practices, patterns, and examples"
    )]
    public static Task<string> ComprehensiveGuide()
    {
        return Task.FromResult(
            @"# Complete Guide: Single-File C# Utility Programs

## What Are File-Based C# Apps?

File-based C# apps are single .cs files that can be run directly with `dotnet run app.cs`. They use:
- **Top-level statements** - No class/Main boilerplate
- **`#:package` directive** - Inline NuGet package declarations (no .csproj)
- **`#:sdk` directive** - Specialized SDKs (web apps, etc.)

Perfect for:
- Quick utility programs
- Command-line tools
- Data processing scripts
- Automation tasks
- Web APIs (with `#:sdk Microsoft.NET.Sdk.Web`)

## Two-Phase Workflow

**Phase 1: Test with nugetPackages parameter**
```
// Rapid testing with inline package loading
EvaluateCsharp(
    code: ""using Humanizer; \""test\"".Humanize()"",
    nugetPackages: [{packageName: ""Humanizer"", version: ""2.14.1""}]
)
```

**Phase 2: Finalize with #:package directive**
```csharp
// text-humanizer.cs - Self-contained production utility
#:package Humanizer@2.14.1

using Humanizer;

var text = args.Length > 0 ? args[0] : ""test"";
Console.WriteLine(text.Humanize());
```

## Essential Tools

**Code Execution:**
- `EvaluateCsharp(code, createContext?, nugetPackages?)` - Execute and test
- `ValidateCsharp(code, contextId?)` - Check syntax before running

**Documentation:**
- Resource: `doc://{symbol}` - XML docs (e.g., doc://System.String)
- `GetDocumentation(symbolName)` - Get docs via tool (if resources unsupported)

**Packages:**
- Resource: `nuget://search?q={query}` - Find packages
- `nugetPackages` parameter - Test packages inline in EvaluateCsharp
- `#:package` directive - Final self-contained apps

## Program Patterns

### 1. Simple Utility (No Arguments)
```csharp
// datestamp.cs
Console.WriteLine($""Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"");
```

### 2. Command-Line Arguments
```csharp
// greet.cs
var args = Environment.GetCommandLineArgs();
var name = args.Length > 1 ? args[1] : ""World"";
Console.WriteLine($""Hello, {name}!"");
return 0;
```

### 3. File Processing with Error Handling
```csharp
// wordcount.cs
using System.IO;

if (args.Length < 2)
{
    Console.WriteLine(""Usage: wordcount <filename>"");
    return 1;
}

var filename = args[1];
if (!File.Exists(filename))
{
    Console.WriteLine($""File not found: {filename}"");
    return 1;
}

try
{
    var text = File.ReadAllText(filename);
    var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, 
        StringSplitOptions.RemoveEmptyEntries);
    Console.WriteLine($""Word count: {words.Length}"");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($""Error: {ex.Message}"");
    return 1;
}
```

### 4. Async Operations
```csharp
// fetch.cs
using System.Net.HttpClient;

if (args.Length < 2)
{
    Console.WriteLine(""Usage: fetch <url>"");
    return 1;
}

var client = new HttpClient();
var content = await client.GetStringAsync(args[1]);
Console.WriteLine(content);
return 0;
```

### 5. Self-Contained with Package
```csharp
// json-pretty.cs
#:package Newtonsoft.Json@13.0.3

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

if (args.Length < 2)
{
    Console.WriteLine(""Usage: json-pretty <input.json>"");
    return 1;
}

var json = File.ReadAllText(args[1]);
var formatted = JToken.Parse(json).ToString(Formatting.Indented);
Console.WriteLine(formatted);
return 0;
```

### 6. Web App with SDK Directive
```csharp
// web-server.cs
#:sdk Microsoft.NET.Sdk.Web

var app = WebApplication.Create(args);
app.MapGet(""/"", () => ""Hello from a single-file web app!"");
app.Run();
```

## Iterative Development

**Build incrementally with contextId:**
```
// 1. Start with structure
EvaluateCsharp(
    code: ""using System.IO;"", 
    createContext: true
)
→ Returns contextId

// 2. Test core logic
EvaluateCsharp(
    code: ""var files = Directory.GetFiles(\"".\"");"",
    contextId: ""<id>""
)

// 3. Iterate on output
EvaluateCsharp(
    code: ""foreach (var file in files) Console.WriteLine(Path.GetFileName(file));"",
    contextId: ""<id>""
)

// 4. Combine into complete program for final .cs file
```

## Validation Strategy

**Always validate before complex operations:**
```
// 1. Check syntax first (fast, safe)
ValidateCsharp(code: ""...<complete program>..."")

// 2. Only execute if valid
EvaluateCsharp(code: ""...<complete program>..."")
```

**Context-aware validation:**
```
// Without context - syntax check only
ValidateCsharp(code: ""x * 2"")
→ Error: 'x' does not exist

// With context - validates against session state
EvaluateCsharp(code: ""var x = 10;"", contextId: ""session1"")
ValidateCsharp(code: ""x * 2"", contextId: ""session1"")
→ { isValid: true }
```

## Complete Program Checklist

✓ **Package directives** at the very top (`#:package` if needed)
✓ **SDK directive** if specialized (`#:sdk` for web, etc.)
✓ **Using directives** after package/SDK directives
✓ **Top-level statements** (no class/Main)
✓ **Error handling** for robustness
✓ **Argument validation** if using args
✓ **Console output** for user feedback
✓ **Return codes** for success/failure (0 = success, non-zero = error)
✓ **Comments** for clarity
✓ **Single file** - complete and self-contained

## Best Practices

- **One file, one purpose:** Keep utilities focused
- **Test incrementally:** Build and test step by step
- **Use `#:package` for dependencies:** Makes files self-contained (no .csproj)
- **Test with nugetPackages:** Verify package in testing → Finalize with `#:package`
- **Use standard libraries:** Avoid dependencies when possible
- **Handle edge cases:** Empty input, missing files, etc.
- **Clear output:** Users should understand what happened
- **Exit codes:** 0 for success, non-zero for errors
- **Top-level statements:** Simpler than class-based structure
- **Validate first:** Use ValidateCsharp before executing complex code

## Documentation Lookup

**Query APIs before coding:**
```
// Look up File operations
doc://System.IO.File

// Look up string methods
doc://System.String

// Look up LINQ
doc://System.Linq.Enumerable
```

## Tips for Success

- **Start simple:** Begin with Console.WriteLine, then add complexity
- **Import namespaces:** Add `using` directives at the top
- **Iterate:** Build programs step by step using contextId
- **Validate first:** Use ValidateCsharp before executing complex code
- **Complete programs:** Aim for runnable .cs files, not just code snippets
- **Modern syntax:** Use .NET 10 directives for self-contained apps"
        );
    }

    /// <summary>
    /// Complete guide for using NuGet packages in file-based C# apps
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Complete guide for discovering, testing, and integrating NuGet packages into single-file C# utility programs"
    )]
    public static Task<string> PackageGuide()
    {
        return Task.FromResult(
            @"# NuGet Package Guide for Single-File C# Utilities

## Two-Phase Package Workflow

### Phase 1: Testing (nugetPackages parameter)
```
// Rapid testing with inline package loading
EvaluateCsharp(
    code: @""
        using Newtonsoft.Json;
        var obj = new { Name = \""Test\"" };
        Console.WriteLine(JsonConvert.SerializeObject(obj));
    "",
    nugetPackages: [{packageName: ""Newtonsoft.Json"", version: ""13.0.3""}]
)
→ Tests package functionality immediately
```

### Phase 2: Production (#:package directive)
```csharp
// json-formatter.cs - Self-contained production utility
#:package Newtonsoft.Json@13.0.3

using System;
using System.IO;
using Newtonsoft.Json;

if (args.Length < 2)
{
    Console.WriteLine(""Usage: json-formatter <input.json>"");
    return 1;
}

var json = File.ReadAllText(args[1]);
var formatted = JsonConvert.SerializeObject(
    JsonConvert.DeserializeObject(json), 
    Formatting.Indented
);
Console.WriteLine(formatted);
return 0;
```
**Run with:** `dotnet run json-formatter.cs input.json` (no .csproj needed!)

## Discovery Strategy

**1. Search packages via resource:**
```
Resource: nuget://search?q=json+serialization
```

**2. Check version history:**
```
Resource: nuget://packages/Newtonsoft.Json/versions
```

**3. Read documentation:**
```
Resource: nuget://packages/Newtonsoft.Json/readme
```

## Complete Utility Examples

### 1. JSON File Processor
```csharp
// json-pretty.cs
#:package Newtonsoft.Json@13.0.3

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

if (args.Length < 2)
{
    Console.WriteLine(""Usage: json-pretty <input.json> [output.json]"");
    return 1;
}

var inputFile = args[1];
var outputFile = args.Length > 2 ? args[2] : null;

try
{
    var json = File.ReadAllText(inputFile);
    var obj = JToken.Parse(json);
    var formatted = obj.ToString(Formatting.Indented);
    
    if (outputFile != null)
    {
        File.WriteAllText(outputFile, formatted);
        Console.WriteLine($""Formatted JSON written to: {outputFile}"");
    }
    else
    {
        Console.WriteLine(formatted);
    }
    
    return 0;
}
catch (JsonException ex)
{
    Console.WriteLine($""JSON Error: {ex.Message}"");
    return 1;
}
catch (IOException ex)
{
    Console.WriteLine($""File Error: {ex.Message}"");
    return 1;
}
```

### 2. HTTP API Client
```csharp
// github-info.cs
#:package Flurl.Http@4.0.0

using System;
using Flurl.Http;

if (args.Length < 2)
{
    Console.WriteLine(""Usage: github-info <owner/repo>"");
    Console.WriteLine(""Example: github-info dotnet/runtime"");
    return 1;
}

var repo = args[1];
var url = $""https://api.github.com/repos/{repo}"";

try
{
    var result = await url
        .WithHeader(""User-Agent"", ""Roslyn-Stone"")
        .GetJsonAsync<dynamic>();
    
    Console.WriteLine($""Repository: {result.full_name}"");
    Console.WriteLine($""Description: {result.description}"");
    Console.WriteLine($""Stars: {result.stargazers_count}"");
    Console.WriteLine($""Forks: {result.forks_count}"");
    Console.WriteLine($""Language: {result.language}"");
    Console.WriteLine($""URL: {result.html_url}"");
    
    return 0;
}
catch (FlurlHttpException ex)
{
    Console.WriteLine($""HTTP Error: {ex.Message}"");
    return 1;
}
```

### 3. CSV Data Analyzer
```csharp
// csv-stats.cs
#:package CsvHelper@30.0.1

using System;
using System.IO;
using System.Linq;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

if (args.Length < 2)
{
    Console.WriteLine(""Usage: csv-stats <file.csv>"");
    return 1;
}

var filename = args[1];
if (!File.Exists(filename))
{
    Console.WriteLine($""File not found: {filename}"");
    return 1;
}

try
{
    using var reader = new StreamReader(filename);
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
    };
    using var csv = new CsvReader(reader, config);
    
    var records = csv.GetRecords<dynamic>().ToList();
    
    Console.WriteLine($""Total Records: {records.Count}"");
    
    if (records.Any())
    {
        var firstRecord = records.First() as IDictionary<string, object>;
        Console.WriteLine($""Columns: {firstRecord.Keys.Count}"");
        Console.WriteLine(""Column Names:"");
        foreach (var key in firstRecord.Keys)
            Console.WriteLine($""  - {key}"");
    }
    
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($""Error: {ex.Message}"");
    return 1;
}
```

### 4. Text Humanizer
```csharp
// humanize.cs
#:package Humanizer@2.14.1

using System;
using Humanizer;

var text = args.Length > 1 ? string.Join("" "", args[1..]) : ""HelloWorld"";
Console.WriteLine($""Original: {text}"");
Console.WriteLine($""Humanized: {text.Humanize()}"");
Console.WriteLine($""Titleized: {text.Titleize()}"");
return 0;
```

## Common Package Categories

**Data Formats:**
- **JSON**: Newtonsoft.Json (nuget://packages/Newtonsoft.Json/readme)
- **CSV**: CsvHelper (nuget://packages/CsvHelper/readme)
- **YAML**: YamlDotNet
- **Markdown**: Markdig

**HTTP/Web:**
- **HTTP Client**: Flurl.Http (nuget://packages/Flurl.Http/readme)
- **REST APIs**: RestSharp

**Text Processing:**
- **Humanizer**: String transformations (nuget://packages/Humanizer/readme)
- **MoreLINQ**: Extended LINQ operations

**Command-Line:**
- **Argument Parsing**: CommandLineParser
- **Terminal UI**: Spectre.Console

**Testing/Mock Data:**
- **Bogus**: Fake data generation

## Best Practices

✓ **Test with nugetPackages:** Verify functionality before creating final app
✓ **Use `#:package` for final apps:** Makes files self-contained (no .csproj)
✓ **Choose stable versions:** Avoid prerelease packages
✓ **Check download counts:** Popular = reliable
✓ **Read README first:** Understand API before using
✓ **Minimal dependencies:** Use built-in libraries when possible
✓ **Package placement:** `#:package` must be at the very top of the file
✓ **Error handling:** Utilities should handle failures gracefully
✓ **User feedback:** Clear console output about what's happening

## Troubleshooting

**Package not found?**
- Verify spelling via `nuget://search?q={name}`
- Check versions via `nuget://packages/{id}/versions`

**Using directive errors?**
- Add `using` statement after `#:package` directive
- Check README for correct namespace
- Example: `#:package Newtonsoft.Json@13.0.3` then `using Newtonsoft.Json;`

**Testing vs Production:**
- **Testing:** Use `nugetPackages` parameter in EvaluateCsharp
- **Production:** Use `#:package` directive in final .cs file"
        );
    }

    /// <summary>
    /// Guide for debugging compilation errors effectively
    /// </summary>
    [McpServerPrompt]
    [Description("Learn how to use ValidateCsharp effectively to debug compilation errors")]
    public static Task<string> DebuggingErrors()
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
- `doc://{symbol}` - Check API usage
- `repl://sessions/{contextId}/state` - Check session variables"
        );
    }
}
