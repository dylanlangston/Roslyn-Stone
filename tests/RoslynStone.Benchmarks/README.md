# Roslyn-Stone Benchmarks

This project contains performance benchmarks for Roslyn-Stone using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Running Benchmarks

### Run All Benchmarks

```bash
dotnet run --project tests/RoslynStone.Benchmarks --configuration Release
```

### Run Specific Benchmark

```bash
dotnet run --project tests/RoslynStone.Benchmarks --configuration Release -- --filter *RoslynScriptingServiceBenchmarks*
```

### Using Cake Build Script

```bash
dotnet cake --target=Benchmark
```

## Available Benchmarks

### RoslynScriptingServiceBenchmarks
- `ExecuteSimpleExpression` - Basic arithmetic operations
- `ExecuteVariableAssignment` - Variable declaration and usage
- `ExecuteLinqQuery` - LINQ query execution
- `ExecuteComplexExpression` - Complex data structure manipulation
- `ExecuteWithStringManipulation` - String operations

### CompilationServiceBenchmarks
- `ValidateSimpleExpression` - Code validation of simple expressions
- `ValidateComplexCode` - Code validation of complex code blocks
- `ValidateWithError` - Validation with compilation errors
- `CompileToAssembly` - Full assembly compilation

### NuGetServiceBenchmarks
- `SearchPackages` - Package search operations
- `GetPackageVersions` - Retrieve package version information
- `GetPackageReadme` - Fetch package README files

## Benchmark Results

Benchmark results are saved to `./artifacts/benchmarks/` and include:
- Execution times (Min, Max, Mean, Median)
- Memory allocations
- Statistical analysis

## Tips

- Always run benchmarks in **Release** configuration
- Close other applications to minimize interference
- Run multiple iterations for consistent results
- Compare results across different machines cautiously
