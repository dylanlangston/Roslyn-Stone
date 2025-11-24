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
    /// Quick start guide for creating single-file C# utility programs
    /// </summary>
    [McpServerPrompt]
    [Description("Get the bare minimum to start creating single-file C# utility programs right now")]
    public static Task<string> QuickStartRepl()
    {
        return Task.FromResult(
            @"# Quick Start: Single-File C# Utility Programs

**Create a simple utility:**
```
EvaluateCsharp(code: ""Console.WriteLine(\""Hello, World!\"");"")
→ { success: true, output: ""Hello, World!\n"" }
```

**File-based app pattern** - top-level statements:
```
// This is how a runnable .cs file looks
Console.WriteLine(""Starting utility..."");
var files = Directory.GetFiles(""."");
foreach (var file in files)
    Console.WriteLine(file);
```

**Iterative development** - build incrementally:
```
// Step 1: Define structure
EvaluateCsharp(code: ""using System.IO;"", createContext: true)
→ Returns contextId

// Step 2: Add logic
EvaluateCsharp(code: ""var files = Directory.GetFiles(\"".\"");"", contextId: ""<id>"")

// Step 3: Output results
EvaluateCsharp(code: ""foreach (var f in files) Console.WriteLine(f);"", contextId: ""<id>"")
```

**Key Tools:**
- `EvaluateCsharp` - Execute and test utility code
- `ValidateCsharp` - Check syntax before running
- Resource: `doc://{symbol}` - Look up .NET APIs (e.g., doc://System.IO.File)
- `LoadNuGetPackage` - Add libraries for extended functionality

**Resources:**
- `doc://{symbol}` - API documentation
- `nuget://search?q={query}` - Find packages

**Workflow:**
1. Look up APIs → 2. Write utility code → 3. Test → 4. Refine → 5. Complete single-file program"
        );
    }

    /// <summary>
    /// Comprehensive introduction to creating single-file C# utility programs
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Get a comprehensive introduction to creating single-file C# utility programs using Roslyn-Stone"
    )]
    public static Task<string> GetStartedWithCsharpRepl()
    {
        return Task.FromResult(
            @"# Creating Single-File C# Utility Programs with Roslyn-Stone

## What Are File-Based C# Apps?

File-based C# apps are single .cs files that can be run directly with `dotnet run app.cs`. They use **top-level statements** (no class/Main boilerplate), making them perfect for:
- Quick utility programs
- Command-line tools
- Data processing scripts
- Automation tasks

## Core Features
- **Execute C# code** - Full .NET 10 support with async/await
- **Iterative development** - Build programs step by step
- **Actionable errors** - Line numbers, suggestions, error codes
- **API documentation** - XML docs via `doc://{symbol}` resources
- **NuGet packages** - Add libraries for extended functionality

## File-Based App Pattern

**Complete single-file utility:**
```csharp
// fileinfo.cs - List files in current directory
using System.IO;

Console.WriteLine(""Files in current directory:"");
var files = Directory.GetFiles(""."");
foreach (var file in files)
{
    var info = new FileInfo(file);
    Console.WriteLine($""{info.Name} - {info.Length} bytes"");
}
```

**Run with:** `dotnet run fileinfo.cs`

## Development Workflow

**Iterative approach** - build incrementally:
```
// Step 1: Set up imports and structure
EvaluateCsharp(code: ""using System.IO;"", createContext: true)
→ Returns contextId for session

// Step 2: Test core logic
EvaluateCsharp(code: ""var files = Directory.GetFiles(\"".\"");"", contextId: ""<id>"")

// Step 3: Iterate on output
EvaluateCsharp(code: @""
foreach (var file in files)
{
    var info = new FileInfo(file);
    Console.WriteLine($\""{info.Name} - {info.Length} bytes\"");
}
"", contextId: ""<id>"")
```

## Essential Tools

**Code Execution:**
- `EvaluateCsharp(code, createContext?)` - Execute and test code
- `ValidateCsharp(code)` - Check syntax before running

**Documentation:**
- Resource: `doc://{symbol}` - Get XML docs (e.g., doc://System.IO.File, doc://System.Linq.Enumerable)

**Packages:**
- `LoadNuGetPackage(packageName, version?)` - Add libraries
- Resources: `nuget://search?q={query}` - Find packages

## Example Utility Programs

**1. Simple calculator:**
```csharp
var args = Environment.GetCommandLineArgs();
if (args.Length < 4)
{
    Console.WriteLine(""Usage: calc <num1> <op> <num2>"");
    return;
}

var num1 = double.Parse(args[1]);
var op = args[2];
var num2 = double.Parse(args[3]);

var result = op switch
{
    ""+"" => num1 + num2,
    ""-"" => num1 - num2,
    ""*"" => num1 * num2,
    ""/"" => num1 / num2,
    _ => throw new InvalidOperationException(""Unknown operator"")
};

Console.WriteLine($""{num1} {op} {num2} = {result}"");
```

**2. JSON file processor:**
```csharp
// Requires: LoadNuGetPackage(""Newtonsoft.Json"")
using System.IO;
using Newtonsoft.Json;

var jsonFile = ""data.json"";
if (!File.Exists(jsonFile))
{
    Console.WriteLine($""File not found: {jsonFile}"");
    return;
}

var json = File.ReadAllText(jsonFile);
var data = JsonConvert.DeserializeObject<dynamic>(json);
Console.WriteLine($""Loaded {data.Count} items"");
```

**3. Async HTTP client:**
```csharp
using System.Net.Http;

var client = new HttpClient();
var response = await client.GetStringAsync(""https://api.github.com/repos/dotnet/runtime"");
Console.WriteLine(response);
```

## Key Capabilities

**Console Output:**
```
EvaluateCsharp(code: ""Console.WriteLine(\""Debug\""); Console.WriteLine(\""Result\"");"")
→ { success: true, output: ""Debug\nResult\n"" }
```

**Error Feedback:**
```
ValidateCsharp(code: ""string x = 123;"")
→ { isValid: false, issues: [{ code: ""CS0029"", line: 1, message: ""..."" }] }
```

**Async Support:**
```
EvaluateCsharp(code: ""await Task.Delay(100); Console.WriteLine(\""Done\"");"")
```

## Workflow Pattern

**Query → Build → Test → Refine:**
1. Look up API: `doc://System.IO.File`
2. Validate syntax: `ValidateCsharp(code: ""..."")`
3. Execute code: `EvaluateCsharp(code: ""..."")`
4. Check result and refine
5. Complete single-file program

## Tips for Success

- **Start simple:** Begin with Console.WriteLine, then add complexity
- **Use top-level statements:** No need for class Program { static void Main() }
- **Import namespaces:** Add `using` directives at the top
- **Validate first:** Use ValidateCsharp before executing complex code
- **Iterate:** Build programs step by step using contextId
- **Complete programs:** Aim for runnable .cs files, not just code snippets"
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
- `doc://{symbol}` - Check API usage
- `repl://sessions/{contextId}/state` - Check session variables"
        );
    }

    /// <summary>
    /// Best practices and patterns for creating single-file C# utility programs
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Learn best practices for creating single-file C# utility programs with iterative development and rapid prototyping"
    )]
    public static Task<string> ReplBestPractices()
    {
        return Task.FromResult(
            @"# Best Practices: Creating Single-File C# Utility Programs

## File-Based App Structure

**Top-level statements pattern:**
```csharp
// utility.cs - No class/Main boilerplate needed
using System.IO;
using System.Linq;

// Direct code execution
Console.WriteLine(""Starting utility..."");
var files = Directory.GetFiles(""."").OrderBy(f => f);
foreach (var file in files)
    Console.WriteLine(file);
```

**Run with:** `dotnet run utility.cs`

## Development Approach

**Iterative building** - develop incrementally:
```
// 1. Start with structure
EvaluateCsharp(code: @""
using System;
using System.IO;
"", createContext: true)
→ Returns contextId

// 2. Add core logic
EvaluateCsharp(code: @""
var files = Directory.GetFiles(\"".\"");
"", contextId: ""<id>"")

// 3. Test output format
EvaluateCsharp(code: @""
foreach (var file in files)
    Console.WriteLine(Path.GetFileName(file));
"", contextId: ""<id>"")

// 4. Combine into complete program
var finalProgram = @""
using System;
using System.IO;

var files = Directory.GetFiles(\"".\"");
foreach (var file in files)
    Console.WriteLine(Path.GetFileName(file));
"";
```

## Program Patterns

**1. Simple utility (no arguments):**
```csharp
// datestamp.cs
Console.WriteLine($""Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"");
```

**2. Command-line arguments:**
```csharp
// greet.cs
var args = Environment.GetCommandLineArgs();
var name = args.Length > 1 ? args[1] : ""World"";
Console.WriteLine($""Hello, {name}!"");
```

**3. File processing:**
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

var text = File.ReadAllText(filename);
var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, 
    StringSplitOptions.RemoveEmptyEntries);
Console.WriteLine($""Word count: {words.Length}"");
return 0;
```

**4. Async operations:**
```csharp
// fetch.cs
using System.Net.Http;

var client = new HttpClient();
var url = args.Length > 1 ? args[1] : ""https://api.github.com"";
var content = await client.GetStringAsync(url);
Console.WriteLine(content);
```

## Validation Strategy

**Always validate before complex operations:**
```
// 1. Check syntax first (fast, safe)
ValidateCsharp(code: ""...<complete program>..."")

// 2. Only execute if valid
EvaluateCsharp(code: ""...<complete program>..."")
```

## Using NuGet Packages

**Add functionality with packages:**
```
// 1. Load package
LoadNuGetPackage(packageName: ""Newtonsoft.Json"")

// 2. Use in program
EvaluateCsharp(code: @""
using Newtonsoft.Json;
using System.IO;

var data = new { Name = \""Alice\"", Age = 30 };
var json = JsonConvert.SerializeObject(data, Formatting.Indented);
Console.WriteLine(json);
"")
```

## Console Output & Return Values

**Console for user output:**
```csharp
// info.cs
Console.WriteLine(""System Information:"");
Console.WriteLine($""OS: {Environment.OSVersion}"");
Console.WriteLine($""CLR: {Environment.Version}"");
Console.WriteLine($""Directory: {Environment.CurrentDirectory}"");
```

**Return for exit codes:**
```csharp
// checker.cs
if (File.Exists(""config.json""))
{
    Console.WriteLine(""Config found"");
    return 0;  // Success
}
else
{
    Console.WriteLine(""Config missing"");
    return 1;  // Error
}
```

## Error Handling

**Robust utilities handle errors:**
```csharp
// safe-read.cs
using System;
using System.IO;

try
{
    var content = File.ReadAllText(""data.txt"");
    Console.WriteLine(content);
    return 0;
}
catch (FileNotFoundException)
{
    Console.WriteLine(""Error: File not found"");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($""Error: {ex.Message}"");
    return 2;
}
```

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

## Complete Program Checklist

✓ **Using directives** at the top
✓ **Top-level statements** (no class/Main)
✓ **Error handling** for robustness
✓ **Console output** for user feedback
✓ **Return codes** for success/failure (optional)
✓ **Argument validation** if using args
✓ **Comments** for clarity
✓ **Single file** - complete and runnable

## Tips

- **One file, one purpose:** Keep utilities focused
- **Test incrementally:** Build and test step by step
- **Use standard libraries:** Avoid dependencies when possible
- **Handle edge cases:** Empty input, missing files, etc.
- **Clear output:** Users should understand what happened
- **Exit codes:** 0 for success, non-zero for errors
- **Top-level statements:** Simpler than class-based structure"
        );
    }

    /// <summary>
    /// Guide for using NuGet packages in single-file utility programs
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Learn how to discover, load, and use NuGet packages in single-file C# utility programs for extended functionality"
    )]
    public static Task<string> WorkingWithPackages()
    {
        return Task.FromResult(
            @"# Using NuGet Packages in Single-File C# Utilities

## Package Workflow

**1. Search** - Find packages via resource:
```
Resource: nuget://search?q=json+serialization
```

**2. Research** - Get package info via resources:
```
Resource: nuget://packages/Newtonsoft.Json/versions

Resource: nuget://packages/Newtonsoft.Json/readme
```

**3. Load** - Add package:
```
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""13.0.3"")
// Version is optional - omit for latest stable
```

**4. Use** - Create utility program:
```csharp
// json-formatter.cs
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

## Complete Example: CSV Processing Utility

```
// Step 1: Find package
Resource: nuget://search?q=csv+parser

// Step 2: Check versions (avoid prereleases)
Resource: nuget://packages/CsvHelper/versions

// Step 3: Read docs to understand usage
Resource: nuget://packages/CsvHelper/readme

// Step 4: Load stable version
LoadNuGetPackage(packageName: ""CsvHelper"", version: ""30.0.1"")

// Step 5: Create utility program
EvaluateCsharp(code: @""
using System;
using System.IO;
using System.Globalization;
using CsvHelper;

// csv-reader.cs - Read and display CSV file
if (args.Length < 2)
{
    Console.WriteLine(\""Usage: csv-reader <file.csv>\"");
    return 1;
}

var filename = args[1];
if (!File.Exists(filename))
{
    Console.WriteLine($\""File not found: {filename}\"");
    return 1;
}

using var reader = new StreamReader(filename);
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

var records = csv.GetRecords<dynamic>();
foreach (var record in records)
{
    Console.WriteLine(record);
}

return 0;
"")
```

## Common Packages for Utilities

**Data Formats:**
- **JSON**: Newtonsoft.Json (nuget://packages/Newtonsoft.Json/readme)
- **CSV**: CsvHelper (nuget://packages/CsvHelper/readme)
- **YAML**: YamlDotNet (nuget://packages/YamlDotNet/readme)

**HTTP/Web:**
- **HTTP Client**: Flurl.Http (nuget://packages/Flurl.Http/readme)
- **REST APIs**: RestSharp (nuget://packages/RestSharp/readme)

**Text Processing:**
- **Humanizer**: Humanizer (nuget://packages/Humanizer/readme)
- **String Utilities**: Humanizer.Core

**File Operations:**
- **Compression**: SharpZipLib, DotNetZip
- **Excel**: EPPlus, NPOI

**Command-Line:**
- **Argument Parsing**: CommandLineParser
- **Terminal UI**: Spectre.Console

## Example Utilities

**1. JSON file validator:**
```csharp
// json-validate.cs
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var file = args.Length > 1 ? args[1] : ""data.json"";
try
{
    var json = File.ReadAllText(file);
    JToken.Parse(json);
    Console.WriteLine($""✓ Valid JSON: {file}"");
    return 0;
}
catch (JsonException ex)
{
    Console.WriteLine($""✗ Invalid JSON: {ex.Message}"");
    return 1;
}
```

**2. HTTP status checker:**
```csharp
// http-check.cs  
using System;
using System.Net.Http;

var url = args.Length > 1 ? args[1] : ""https://github.com"";
using var client = new HttpClient();

try
{
    var response = await client.GetAsync(url);
    Console.WriteLine($""{url} returned {(int)response.StatusCode} {response.StatusCode}"");
    return response.IsSuccessStatusCode ? 0 : 1;
}
catch (Exception ex)
{
    Console.WriteLine($""Error: {ex.Message}"");
    return 1;
}
```

**3. Text humanizer:**
```csharp
// humanize.cs
using System;
using Humanizer;

var text = args.Length > 1 ? string.Join("" "", args[1..]) : ""HelloWorld"";
Console.WriteLine($""Original: {text}"");
Console.WriteLine($""Humanized: {text.Humanize()}"");
Console.WriteLine($""Titleized: {text.Titleize()}"");
```

## Best Practices

- **Choose stable versions:** Avoid prerelease packages
- **Check download counts:** Popular = reliable
- **Read README first:** Understand API before loading
- **Test simple example:** Verify package works
- **Minimal dependencies:** Use built-in libraries when possible
- **Dependencies auto-load:** Don't manually load package dependencies

## Troubleshooting

**Package not found?**
- Verify spelling via `nuget://search?q={name}`
- Check versions via `nuget://packages/{id}/versions`

**Using directive errors?**
- Add `using` statement after loading package
- Check README for correct namespace
- Example: `using Newtonsoft.Json;`

**Version conflict?**
- Reset and reload: `ResetRepl()` then `LoadNuGetPackage(...)`

## Package Persistence

Packages remain loaded until reset:
```
LoadNuGetPackage(packageName: ""Humanizer"")
// Available in all subsequent executions

ResetRepl()  
// Clears packages - must reload
```"
        );
    }

    /// <summary>
    /// Comprehensive guide for integrating packages into single-file utility programs
    /// </summary>
    [McpServerPrompt]
    [Description("Deep dive into NuGet package integration for building single-file C# utility programs with detailed examples")]
    public static Task<string> PackageIntegrationGuide()
    {
        return Task.FromResult(
            @"# NuGet Package Integration Guide for Single-File Utilities

## Discovery Strategy

**Use resource-driven search:**
```
Resource: nuget://search?q=http+client
```

**Evaluate results:**
- `downloadCount` - Popularity indicator
- `description` - Feature overview
- `latestVersion` - Current stable release

## Investigation Phase

**Check version history:**
```
Resource: nuget://packages/Flurl.Http/versions
```
- Avoid deprecated versions
- Prefer stable over prerelease
- Note download counts per version

**Read documentation:**
```
Resource: nuget://packages/Flurl.Http/readme
```
README typically includes:
- Installation guide
- Quick start examples
- API overview
- Migration notes

## Integration Workflow

**Load package:**
```
// Latest stable
LoadNuGetPackage(packageName: ""Humanizer"")

// Specific version
LoadNuGetPackage(packageName: ""Humanizer"", version: ""2.14.1"")
```

**Create utility program:**
```csharp
// word-humanizer.cs
using System;
using Humanizer;

var text = args.Length > 1 ? args[1] : ""PascalCaseText"";
Console.WriteLine($""Original: {text}"");
Console.WriteLine($""Humanized: {text.Humanize()}"");
```

## Complete Utility Examples

### 1. JSON File Processor

**Load package:**
```
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""13.0.3"")
```

**Create utility:**
```csharp
// json-pretty.cs - Pretty-print JSON files
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

**Load package:**
```
LoadNuGetPackage(packageName: ""Flurl.Http"")
```

**Create utility:**
```csharp
// github-info.cs - Fetch GitHub repository info
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

**Load package:**
```
LoadNuGetPackage(packageName: ""CsvHelper"")
```

**Create utility:**
```csharp
// csv-stats.cs - Analyze CSV file
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

### 4. Fake Data Generator

**Load package:**
```
LoadNuGetPackage(packageName: ""Bogus"")
```

**Create utility:**
```csharp
// generate-users.cs - Generate fake user data
using System;
using Bogus;

var count = args.Length > 1 ? int.Parse(args[1]) : 10;

var userFaker = new Faker<User>()
    .RuleFor(u => u.FirstName, f => f.Name.FirstName())
    .RuleFor(u => u.LastName, f => f.Name.LastName())
    .RuleFor(u => u.Email, (f, u) => 
        f.Internet.Email(u.FirstName, u.LastName))
    .RuleFor(u => u.Age, f => f.Random.Int(18, 80));

var users = userFaker.Generate(count);

Console.WriteLine($""Generated {count} users:"");
foreach (var user in users)
    Console.WriteLine($""{user.FirstName} {user.LastName} <{user.Email}> (Age: {user.Age})"");

return 0;

class User
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}
```

## Package Categories for Utilities

**Data Processing:**
- JSON: Newtonsoft.Json, System.Text.Json
- CSV: CsvHelper
- XML: System.Xml.Linq (built-in)
- YAML: YamlDotNet
- Excel: EPPlus, NPOI

**Networking:**
- HTTP: Flurl.Http, RestSharp, Refit
- WebSockets: Websocket.Client
- Email: MailKit

**Text & String Utilities:**
- Humanizer: String transformations
- MoreLINQ: Extended LINQ operations
- Markdown: Markdig

**Command-Line:**
- CommandLineParser: Argument parsing
- Spectre.Console: Rich terminal output
- McMaster.Extensions.CommandLineUtils: CLI framework

**Testing/Mock Data:**
- Bogus: Fake data generation
- AutoFixture: Test data builder

**File & Compression:**
- SharpZipLib: Compression/decompression
- DotNetZip: ZIP operations
- ImageSharp: Image processing

## Development Pattern

**1. Discover package:**
```
nuget://search?q=markdown+parser
```

**2. Research package:**
```
nuget://packages/Markdig/readme
nuget://packages/Markdig/versions
```

**3. Load and test:**
```
LoadNuGetPackage(packageName: ""Markdig"")

// Test basic functionality
EvaluateCsharp(code: @""
using Markdig;
var md = \""# Hello\\n\\nThis is **bold**\"";
var html = Markdown.ToHtml(md);
Console.WriteLine(html);
"")
```

**4. Build utility:**
```csharp
// md2html.cs - Convert Markdown to HTML
using System;
using System.IO;
using Markdig;

if (args.Length < 2)
{
    Console.WriteLine(""Usage: md2html <input.md> [output.html]"");
    return 1;
}

var inputFile = args[1];
var outputFile = args.Length > 2 ? args[2] : Path.ChangeExtension(inputFile, "".html"");

var markdown = File.ReadAllText(inputFile);
var html = Markdown.ToHtml(markdown);

File.WriteAllText(outputFile, html);
Console.WriteLine($""Converted {inputFile} → {outputFile}"");
return 0;
```

## Best Practices

✓ **Start simple:** Test package with basic example first
✓ **Read docs:** Understand API before building utility
✓ **Stable versions:** Prefer stable over prerelease
✓ **Error handling:** Utilities should handle failures gracefully
✓ **User feedback:** Clear console output about what's happening
✓ **Single purpose:** One utility, one task
✓ **Dependencies auto-load:** Don't manually load package dependencies

## Performance Tips

- Load packages once per development session
- Dependencies auto-resolve (don't load manually)
- Avoid loading multiple versions of same package (causes conflicts)
- Use `ResetRepl()` to clear all packages and start fresh

## State & Packages

Packages persist in session:
```
LoadNuGetPackage(packageName: ""Humanizer"")
// Available in all subsequent code executions

ResetRepl()  // Clears packages
// Must reload packages after reset
```"
        );
    }
}
