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
    /// Getting started prompt for new users
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Get a comprehensive introduction to Roslyn-Stone's capabilities and how to use the C# REPL effectively"
    )]
    public static Task<string> GetStartedWithCsharpRepl()
    {
        return Task.FromResult(
            @"# Welcome to Roslyn-Stone: C# REPL for LLMs

Roslyn-Stone is a developer- and LLM-friendly C# REPL (Read-Eval-Print Loop) sandbox. It provides:

## Core Capabilities

1. **Execute C# Code** - Run C# expressions and statements with full .NET 10 support
2. **Stateful REPL** - Variables and types persist between executions
3. **Error Feedback** - Get actionable compilation errors with line numbers and suggestions
4. **Documentation Lookup** - Query XML documentation for .NET types and methods
5. **NuGet Integration** - Search, discover, and load NuGet packages dynamically

## Quick Start Guide

### 1. Simple Expression Evaluation
Use `EvaluateCsharp` to execute C# code:
```csharp
EvaluateCsharp(code: ""2 + 2"")
// Returns: { success: true, returnValue: 4, executionTime: ... }
```

### 2. Stateful Programming
The REPL maintains state between calls:
```csharp
// First call
EvaluateCsharp(code: ""int x = 10; x"")
// Returns: 10

// Second call - x is still in scope
EvaluateCsharp(code: ""x * 2"")
// Returns: 20
```

### 3. Console Output Capture
Console.WriteLine output is captured separately from return values:
```csharp
EvaluateCsharp(code: ""Console.WriteLine(\""Debug info\""); return \""Result\"";"")
// Returns: { returnValue: ""Result"", output: ""Debug info\n"" }
```

### 4. Validation Without Execution
Check syntax before running code:
```csharp
ValidateCsharp(code: ""string x = 123;"")
// Returns: { isValid: false, issues: [{ code: ""CS0029"", message: ""Cannot convert..."" }] }
```

### 5. Documentation Lookup
Get .NET documentation for any type or method:
```csharp
GetDocumentation(symbolName: ""System.String"")
// Returns: { found: true, summary: ""..."", parameters: [...] }
```

### 6. Working with NuGet Packages
Load and use external libraries:
```csharp
// Step 1: Search for packages
SearchNuGetPackages(query: ""Newtonsoft.Json"", take: 5)

// Step 2: Load the package
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""13.0.3"")

// Step 3: Use it in your code
EvaluateCsharp(code: ""using Newtonsoft.Json; JsonConvert.SerializeObject(new { Name = \""Test\"" })"")
```

## Best Practices

1. **Start Simple** - Test with simple expressions before complex code
2. **Use Validation** - Call ValidateCsharp for syntax checking before execution
3. **Check Documentation** - Use GetDocumentation to understand .NET APIs
4. **Manage State** - Use ResetRepl to clear the environment when needed
5. **Handle Errors** - Pay attention to error messages - they include line numbers and suggestions

## Common Patterns

### Iterative Development
```csharp
// 1. Validate your code first
ValidateCsharp(code: ""...<your code>..."")

// 2. If valid, execute it
EvaluateCsharp(code: ""...<your code>..."")

// 3. Check the result and iterate
```

### Exploring New APIs
```csharp
// 1. Look up documentation
GetDocumentation(symbolName: ""System.Linq.Enumerable.Select"")

// 2. Experiment with the API
EvaluateCsharp(code: ""new[] { 1, 2, 3 }.Select(x => x * 2).ToArray()"")
```

### Package Exploration
```csharp
// 1. Search for packages
SearchNuGetPackages(query: ""json"", take: 5)

// 2. Get package details
GetNuGetPackageVersions(packageId: ""Newtonsoft.Json"")
GetNuGetPackageReadme(packageId: ""Newtonsoft.Json"")

// 3. Load and use
LoadNuGetPackage(packageName: ""Newtonsoft.Json"")
```

## Tips for Success

- **Return values**: The last expression in your code is automatically returned
- **Multiple statements**: Separate statements with semicolons; use 'return' for explicit returns
- **Async/await**: Full async support - await works in the REPL
- **Top-level statements**: Write code as if it's inside a Main method
- **Using directives**: Add 'using' statements to import namespaces
- **Console output**: Use Console.WriteLine for debugging output

## Error Recovery

If you encounter errors:
1. Read the error message carefully - it includes line/column information
2. Use ValidateCsharp to check syntax without executing
3. Use ResetRepl if the REPL state becomes inconsistent
4. Use GetDocumentation to verify API usage

You're now ready to use Roslyn-Stone effectively! Start with simple expressions and gradually build up to complex programs."
        );
    }

    /// <summary>
    /// Prompt for iterative code development and experimentation
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Learn best practices for iterative C# code development using the REPL for experimentation and rapid prototyping"
    )]
    public static Task<string> CodeExperimentationWorkflow()
    {
        return Task.FromResult(
            @"# Code Experimentation Workflow with Roslyn-Stone

This prompt guides you through an effective workflow for iterative C# development using the REPL.

## The Experimentation Cycle

### 1. Validate First (Syntax Check)
Before executing code, validate it to catch syntax errors:

```csharp
ValidateCsharp(code: ""your code here"")
```

**Why?** Validation is fast and safe - it checks syntax without executing the code.

**What to check:**
- Look for `isValid: false`
- Review the `issues` array for errors and warnings
- Note line/column numbers for errors
- Read error messages for hints on fixes

### 2. Execute and Observe (Run Code)
Once validation passes, execute the code:

```csharp
EvaluateCsharp(code: ""your code here"")
```

**What to check:**
- `success: true/false` - did it run without errors?
- `returnValue` - the result of your code
- `output` - any Console.WriteLine messages
- `errors` - runtime or compilation errors
- `warnings` - potential issues to address
- `executionTime` - performance metrics

### 3. Iterate and Refine (Build Incrementally)
Use the stateful nature of the REPL to build complexity gradually:

```csharp
// Step 1: Define data structures
EvaluateCsharp(code: ""class Person { public string Name { get; set; } public int Age { get; set; } }"")

// Step 2: Create instances
EvaluateCsharp(code: ""var person = new Person { Name = \""John\"", Age = 30 };"")

// Step 3: Test operations
EvaluateCsharp(code: ""person.Name"")

// Step 4: Build on it
EvaluateCsharp(code: ""var people = new List<Person> { person };"")
```

## Best Practices for Experimentation

### Start Small, Build Big
```csharp
// ❌ Don't: Write everything at once
EvaluateCsharp(code: ""<100 lines of complex code>"")

// ✅ Do: Build incrementally
// Step 1: Test the core logic
EvaluateCsharp(code: ""int Add(int a, int b) => a + b; Add(2, 3)"")

// Step 2: Add complexity
EvaluateCsharp(code: ""int Sum(params int[] numbers) => numbers.Sum();"")

// Step 3: Test edge cases
EvaluateCsharp(code: ""Sum()"")  // Empty array
EvaluateCsharp(code: ""Sum(1, 2, 3, 4, 5)"")  // Multiple values
```

### Use Console.WriteLine for Debugging
```csharp
EvaluateCsharp(code: @""
    var data = new[] { 1, 2, 3, 4, 5 };
    Console.WriteLine($\""Processing {data.Length} items\"");
    
    var result = data.Where(x => x > 2).ToArray();
    Console.WriteLine($\""Found {result.Length} items > 2\"");
    
    return result;
"")
// Check 'output' field for debug messages
// Check 'returnValue' for the actual result
```

### Validate Before Complex Operations
```csharp
// Before running potentially long operations, validate first
ValidateCsharp(code: @""
    using System.Linq;
    var numbers = Enumerable.Range(1, 1000000);
    var result = numbers.Where(n => n % 2 == 0).Sum();
"")

// Only execute if validation passes
```

### Test Error Handling
```csharp
// Test how your code handles errors
EvaluateCsharp(code: @""
    try {
        int.Parse(\""not a number\"");
    }
    catch (Exception ex) {
        Console.WriteLine($\""Caught: {ex.GetType().Name}\"");
        return \""error handled\"";
    }
"")
```

## Progressive Complexity Pattern

### Level 1: Basic Expressions
```csharp
EvaluateCsharp(code: ""2 + 2"")
EvaluateCsharp(code: ""Math.Sqrt(16)"")
EvaluateCsharp(code: ""DateTime.Now.ToString()"")
```

### Level 2: Variable Declaration
```csharp
EvaluateCsharp(code: ""var name = \""Alice\"""")
EvaluateCsharp(code: ""var age = 25"")
EvaluateCsharp(code: ""$\""{name} is {age} years old\"""")
```

### Level 3: Functions and Logic
```csharp
EvaluateCsharp(code: @""
    bool IsPrime(int n) {
        if (n < 2) return false;
        for (int i = 2; i <= Math.Sqrt(n); i++)
            if (n % i == 0) return false;
        return true;
    }
"")
EvaluateCsharp(code: ""IsPrime(17)"")
```

### Level 4: Classes and Objects
```csharp
EvaluateCsharp(code: @""
    class Calculator {
        public int Add(int a, int b) => a + b;
        public int Multiply(int a, int b) => a * b;
    }
"")
EvaluateCsharp(code: ""var calc = new Calculator(); calc.Add(5, 3)"")
```

### Level 5: LINQ and Collections
```csharp
EvaluateCsharp(code: @""
    var numbers = new[] { 1, 2, 3, 4, 5 };
    numbers.Where(n => n % 2 == 0)
           .Select(n => n * n)
           .ToArray()
"")
```

### Level 6: Async Operations
```csharp
EvaluateCsharp(code: @""
    await Task.Delay(100);
    return \""Async complete\"";
"")
```

## Resetting When Needed

When your REPL state becomes cluttered or you want a fresh start:

```csharp
ResetRepl()
// Now start fresh with a clean environment
```

**When to reset:**
- Starting a new experiment
- Conflicting variable names
- Testing different approaches
- After errors that might have corrupted state

## Troubleshooting Tips

1. **Compilation errors**: Use ValidateCsharp first
2. **Runtime errors**: Check the 'errors' field in the response
3. **Unexpected results**: Add Console.WriteLine for debugging
4. **Performance issues**: Check 'executionTime' in the response
5. **State confusion**: Use ResetRepl to start fresh

Remember: The REPL is stateful - variables and types persist between calls. Use this to your advantage for iterative development!"
        );
    }

    /// <summary>
    /// Prompt for working with NuGet packages
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Learn how to discover, load, and use NuGet packages in the C# REPL for extended functionality"
    )]
    public static Task<string> PackageIntegrationGuide()
    {
        return Task.FromResult(
            @"# NuGet Package Integration Guide for Roslyn-Stone

Learn how to extend your C# REPL with external NuGet packages for enhanced functionality.

## The Package Workflow

### 1. Discovery: Find the Right Package
Use `SearchNuGetPackages` to find packages:

```csharp
SearchNuGetPackages(query: ""json"", take: 10)
```

**Search Tips:**
- Use specific keywords (e.g., ""json serialization"", ""http client"", ""csv parser"")
- Check `downloadCount` for popularity
- Read `description` to understand the package
- Note the `latestVersion` for loading

**Common package categories:**
- **JSON**: Newtonsoft.Json, System.Text.Json
- **HTTP**: RestSharp, Flurl.Http
- **Testing**: FluentAssertions, Bogus (fake data)
- **Utilities**: Humanizer, MoreLINQ, CsvHelper
- **Data**: Dapper, AutoMapper, Polly

### 2. Investigation: Learn About the Package

#### Get Available Versions
```csharp
GetNuGetPackageVersions(packageId: ""Newtonsoft.Json"")
```

**What to check:**
- Latest stable version (not prerelease)
- Which versions are deprecated
- Download counts per version

#### Read the Documentation
```csharp
GetNuGetPackageReadme(packageId: ""Newtonsoft.Json"", version: ""13.0.3"")
```

**README contents typically include:**
- Installation instructions
- Quick start examples
- API overview
- Links to full documentation

### 3. Integration: Load and Use the Package

#### Load the Package
```csharp
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""13.0.3"")
```

**Note:** 
- Version is optional - omit it to get the latest stable version
- Loading may take a few seconds for dependency resolution
- Once loaded, the package is available for all subsequent code executions

#### Use the Package
```csharp
EvaluateCsharp(code: @""
    using Newtonsoft.Json;
    
    var person = new { Name = \""Alice\"", Age = 30 };
    var json = JsonConvert.SerializeObject(person);
    
    return json;
"")
```

## Complete Example Workflows

### Example 1: JSON Serialization with Newtonsoft.Json

```csharp
// Step 1: Search for JSON packages
SearchNuGetPackages(query: ""json serialization"", take: 5)

// Step 2: Check versions
GetNuGetPackageVersions(packageId: ""Newtonsoft.Json"")

// Step 3: Read documentation
GetNuGetPackageReadme(packageId: ""Newtonsoft.Json"")

// Step 4: Load the package
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""13.0.3"")

// Step 5: Use it - Serialize
EvaluateCsharp(code: @""
    using Newtonsoft.Json;
    
    var data = new {
        Name = \""John Doe\"",
        Email = \""john@example.com\"",
        Age = 30
    };
    
    return JsonConvert.SerializeObject(data, Formatting.Indented);
"")

// Step 6: Use it - Deserialize
EvaluateCsharp(code: @""
    using Newtonsoft.Json;
    
    string json = \""{\\\""Name\\\"":\\\""John\\\"",\\\""Age\\\"":30}\"";
    dynamic obj = JsonConvert.DeserializeObject(json);
    
    return $\""{obj.Name} is {obj.Age} years old\"";
"")
```

### Example 2: HTTP Requests with Flurl.Http

```csharp
// Step 1: Find HTTP client packages
SearchNuGetPackages(query: ""http client fluent"", take: 5)

// Step 2: Load Flurl.Http
LoadNuGetPackage(packageName: ""Flurl.Http"", version: ""4.0.0"")

// Step 3: Make API requests
EvaluateCsharp(code: @""
    using Flurl.Http;
    
    // Note: In a sandboxed environment, this may not have internet access
    var response = await \""https://api.github.com/users/github\""
        .GetJsonAsync<dynamic>();
    
    return $\""GitHub user: {response.login}\"";
"")
```

### Example 3: Fake Data Generation with Bogus

```csharp
// Step 1: Load Bogus for fake data
LoadNuGetPackage(packageName: ""Bogus"")

// Step 2: Generate test data
EvaluateCsharp(code: @""
    using Bogus;
    
    var faker = new Faker();
    var people = Enumerable.Range(1, 5)
        .Select(_ => new {
            Name = faker.Name.FullName(),
            Email = faker.Internet.Email(),
            Phone = faker.Phone.PhoneNumber()
        })
        .ToList();
    
    return people;
"")
```

### Example 4: String Manipulation with Humanizer

```csharp
// Step 1: Search and load
SearchNuGetPackages(query: ""Humanizer"", take: 3)
LoadNuGetPackage(packageName: ""Humanizer"")

// Step 2: Use Humanizer's extensions
EvaluateCsharp(code: @""
    using Humanizer;
    
    var examples = new[] {
        \""PascalCase\"".Humanize(),
        DateTime.Now.AddDays(-5).Humanize(),
        1234567.ToWords(),
        TimeSpan.FromDays(3).Humanize()
    };
    
    return string.Join(\""\n\"", examples);
"")
```

## Best Practices

### 1. Choose Stable Versions
```csharp
// ✅ Prefer stable versions
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""13.0.3"")

// ⚠️ Be cautious with prereleases
LoadNuGetPackage(packageName: ""SomePackage"", version: ""2.0.0-beta"")
```

### 2. Check Package Popularity
Look for packages with high download counts - they're more likely to be:
- Well-maintained
- Well-documented
- Stable and reliable
- Compatible

### 3. Read Before Loading
Always check the README to understand:
- What the package does
- How to use it
- Any special requirements
- Breaking changes between versions

### 4. Test After Loading
After loading a package, test with a simple example:

```csharp
// Load the package
LoadNuGetPackage(packageName: ""CsvHelper"")

// Test with a simple example
EvaluateCsharp(code: @""
    using CsvHelper;
    Console.WriteLine(\""CsvHelper loaded successfully\"");
    return true;
"")
```

### 5. Manage Dependencies
Some packages have dependencies that are automatically loaded:

```csharp
// Loading this package...
LoadNuGetPackage(packageName: ""Microsoft.Extensions.DependencyInjection"")

// ...also loads its dependencies automatically
// You don't need to load them manually
```

## Common Package Patterns

### Data Processing
```csharp
// CSV: CsvHelper
// XML: System.Xml.Linq (built-in)
// JSON: Newtonsoft.Json or System.Text.Json (built-in)
// YAML: YamlDotNet
```

### HTTP/Network
```csharp
// REST APIs: RestSharp, Flurl.Http, Refit
// HTTP Client: System.Net.Http (built-in)
```

### Utilities
```csharp
// String manipulation: Humanizer
// LINQ extensions: MoreLINQ
// Validation: FluentValidation
// Mapping: AutoMapper
```

### Testing Helpers
```csharp
// Fake data: Bogus, AutoFixture
// Assertions: FluentAssertions
// Mocking: Moq (may not work in REPL due to code generation)
```

## Troubleshooting

### Package Not Loading
1. Check the package ID spelling
2. Verify the version exists
3. Try without specifying a version (gets latest stable)
4. Check if package requires .NET Framework (won't work in .NET 10)

### Using Directives Not Found
After loading, you must add the using directive:

```csharp
// ❌ Won't work
EvaluateCsharp(code: ""JsonConvert.SerializeObject(obj)"")

// ✅ Correct
EvaluateCsharp(code: @""
    using Newtonsoft.Json;
    JsonConvert.SerializeObject(obj)
"")
```

### Version Conflicts
If you need a different version, reset and reload:

```csharp
ResetRepl()
LoadNuGetPackage(packageName: ""Newtonsoft.Json"", version: ""12.0.3"")
```

Remember: Once loaded, a package stays available until you call ResetRepl(). Build on top of loaded packages to create powerful solutions!"
        );
    }

    /// <summary>
    /// Prompt for understanding and fixing errors
    /// </summary>
    [McpServerPrompt]
    [Description(
        "Learn how to interpret compilation errors, runtime errors, and use Roslyn-Stone's debugging features effectively"
    )]
    public static Task<string> DebuggingAndErrorHandling()
    {
        return Task.FromResult(
            @"# Debugging and Error Handling in Roslyn-Stone

Learn how to effectively debug C# code and handle errors in the REPL environment.

## Understanding Error Types

### 1. Compilation Errors (Syntax Errors)
These occur when code doesn't follow C# syntax rules.

**Example:**
```csharp
ValidateCsharp(code: ""string x = 123;"")
// Returns:
// {
//   isValid: false,
//   issues: [{
//     code: ""CS0029"",
//     message: ""Cannot implicitly convert type 'int' to 'string'"",
//     severity: ""Error"",
//     line: 1,
//     column: 12
//   }]
// }
```

**How to fix:**
- Read the error code (CS####)
- Check the line and column number
- Read the message for hints
- Look up the error code online if needed

### 2. Runtime Errors (Exceptions)
These occur during code execution.

**Example:**
```csharp
EvaluateCsharp(code: ""int x = int.Parse(\""not a number\"");"")
// Returns:
// {
//   success: false,
//   errors: [{
//     code: ""System.FormatException"",
//     message: ""Input string was not in a correct format."",
//     ...
//   }]
// }
```

**How to fix:**
- Check what operation failed
- Add try-catch for error handling
- Validate inputs before operations
- Use TryParse methods for conversions

### 3. Warnings
Non-fatal issues that might indicate problems.

**Example:**
```csharp
EvaluateCsharp(code: ""var x = 10; var y = 20;"")
// May return warnings about unused variables
```

## Debugging Workflow

### Step 1: Validate Before Executing

```csharp
// Always validate complex code first
ValidateCsharp(code: @""
    class Person {
        public string Name { get; set; }
        public int Age { get; set; }
        
        public void Greet() {
            Console.WriteLine($\""Hello, I'm {Name}\"");
        }
    }
"")

// Check: isValid should be true before executing
```

### Step 2: Add Console.WriteLine for Visibility

```csharp
EvaluateCsharp(code: @""
    var numbers = new[] { 1, 2, 3, 4, 5 };
    Console.WriteLine($\""Starting with {numbers.Length} numbers\"");
    
    var evens = numbers.Where(n => n % 2 == 0).ToArray();
    Console.WriteLine($\""Found {evens.Length} even numbers\"");
    
    var sum = evens.Sum();
    Console.WriteLine($\""Sum is {sum}\"");
    
    return sum;
"")

// Check 'output' field for debug messages
// Check 'returnValue' for final result
```

### Step 3: Test Incrementally

```csharp
// Test each part separately

// Part 1: Data creation
EvaluateCsharp(code: ""var data = new[] { 1, 2, 3, 4, 5 };"")

// Part 2: Filtering
EvaluateCsharp(code: ""var filtered = data.Where(x => x > 2).ToArray();"")

// Part 3: Check result
EvaluateCsharp(code: ""filtered"")

// Part 4: Further processing
EvaluateCsharp(code: ""filtered.Sum()"")
```

### Step 4: Use Try-Catch for Error Handling

```csharp
EvaluateCsharp(code: @""
    try {
        var result = int.Parse(\""maybe a number\"");
        return result;
    }
    catch (FormatException ex) {
        Console.WriteLine($\""Parse error: {ex.Message}\"");
        return -1; // Default value
    }
    catch (Exception ex) {
        Console.WriteLine($\""Unexpected error: {ex.GetType().Name}\"");
        throw; // Re-throw if unexpected
    }
"")
```

## Common Error Patterns and Fixes

### Error: Variable Not Found
```csharp
// ❌ Problem
EvaluateCsharp(code: ""y + 10"")
// Error: The name 'y' does not exist

// ✅ Solution: Define the variable first
EvaluateCsharp(code: ""var y = 5;"")
EvaluateCsharp(code: ""y + 10"")
```

### Error: Type Conversion
```csharp
// ❌ Problem
EvaluateCsharp(code: ""string x = 123;"")
// Error: Cannot implicitly convert type 'int' to 'string'

// ✅ Solution: Explicit conversion
EvaluateCsharp(code: ""string x = 123.ToString();"")
```

### Error: Null Reference
```csharp
// ❌ Problem
EvaluateCsharp(code: @""
    string text = null;
    return text.Length;
"")
// Error: NullReferenceException

// ✅ Solution: Null checking
EvaluateCsharp(code: @""
    string text = null;
    return text?.Length ?? 0;
"")
```

### Error: Missing Namespace
```csharp
// ❌ Problem
EvaluateCsharp(code: ""var numbers = new List<int>();"")
// Error: The type or namespace name 'List<>' could not be found

// ✅ Solution: Add using directive
EvaluateCsharp(code: @""
    using System.Collections.Generic;
    var numbers = new List<int>();
"")
```

### Error: Async Without Await
```csharp
// ❌ Problem
EvaluateCsharp(code: ""Task.Delay(1000);"")
// Warning: No await

// ✅ Solution: Use await
EvaluateCsharp(code: ""await Task.Delay(1000);"")
```

## Advanced Debugging Techniques

### 1. Inspect Object State
```csharp
EvaluateCsharp(code: @""
    var person = new { Name = \""Alice\"", Age = 30 };
    
    // Inspect using string interpolation
    Console.WriteLine($\""Person: {person.Name}, {person.Age}\"");
    
    // Inspect using JSON serialization (if Newtonsoft.Json is loaded)
    // var json = JsonConvert.SerializeObject(person, Formatting.Indented);
    // Console.WriteLine(json);
    
    return person;
"")
```

### 2. Step Through Logic
```csharp
EvaluateCsharp(code: @""
    int factorial = 1;
    for (int i = 1; i <= 5; i++) {
        factorial *= i;
        Console.WriteLine($\""i={i}, factorial={factorial}\"");
    }
    return factorial;
"")
```

### 3. Test Edge Cases
```csharp
// Test with empty input
EvaluateCsharp(code: @""
    var numbers = new int[0];
    var result = numbers.Sum();
    Console.WriteLine($\""Sum of empty array: {result}\"");
    return result;
"")

// Test with null
EvaluateCsharp(code: @""
    string text = null;
    var result = text?.ToUpper() ?? \""NULL\"";
    Console.WriteLine($\""Result: {result}\"");
    return result;
"")
```

### 4. Performance Debugging
```csharp
EvaluateCsharp(code: @""
    var sw = System.Diagnostics.Stopwatch.StartNew();
    
    // Your code here
    var result = Enumerable.Range(1, 1000000).Sum();
    
    sw.Stop();
    Console.WriteLine($\""Execution time: {sw.ElapsedMilliseconds}ms\"");
    
    return result;
"")

// Also check 'executionTime' in the response
```

## Using GetDocumentation for Debugging

When you encounter unfamiliar APIs or need to understand method signatures:

```csharp
// Look up how to use a method
GetDocumentation(symbolName: ""System.String.Split"")

// Look up a class
GetDocumentation(symbolName: ""System.Collections.Generic.List`1"")

// Look up LINQ methods
GetDocumentation(symbolName: ""System.Linq.Enumerable.Where"")
```

## When to Reset the REPL

Sometimes the REPL state can become inconsistent. Reset when:

```csharp
// Use ResetRepl when:
// - Variables have conflicting values
// - Type definitions conflict
// - You want a fresh start
// - After major errors

ResetRepl()

// Then start fresh
```

## Error Response Structure

Understanding the error response helps with debugging:

```csharp
// For compilation errors (ValidateCsharp):
{
  isValid: false,
  issues: [
    {
      code: ""CS0029"",           // Error code (look up online)
      message: ""Cannot convert..."", // Human-readable message
      severity: ""Error"",        // ""Error"" or ""Warning""
      line: 1,                   // Line number (1-based)
      column: 12                 // Column number (1-based)
    }
  ]
}

// For runtime errors (EvaluateCsharp):
{
  success: false,
  errors: [
    {
      code: ""System.FormatException"",
      message: ""Input string was not in a correct format."",
      ...
    }
  ],
  returnValue: null,
  output: ""...<any console output>...""
}
```

## Tips for Effective Debugging

1. **Start Simple**: Reduce code to minimal failing example
2. **Add Logging**: Use Console.WriteLine liberally
3. **Validate First**: Use ValidateCsharp before EvaluateCsharp
4. **Test Incrementally**: Build up complexity gradually
5. **Read Errors Carefully**: Error messages often contain the solution
6. **Use Documentation**: GetDocumentation for API understanding
7. **Reset When Stuck**: ResetRepl for a fresh start
8. **Handle Exceptions**: Use try-catch for robustness
9. **Check Types**: Ensure type compatibility
10. **Test Edge Cases**: null, empty, zero, negative values

Remember: Debugging is iterative. Use the REPL's stateful nature to test hypotheses quickly!"
        );
    }
}
