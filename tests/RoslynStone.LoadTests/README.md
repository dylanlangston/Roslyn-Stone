# Roslyn-Stone Load Tests

This project contains load tests for the Roslyn-Stone MCP server HTTP transport.

## Overview

The load tests simulate concurrent requests to the MCP server to ensure it can handle multiple simultaneous clients. Tests include:
- Simple expression evaluation
- Variable assignment
- LINQ queries
- NuGet package search

## Prerequisites

The load test requires the RoslynStone.Api server to be running in HTTP mode.

### Start the Server

```bash
cd src/RoslynStone.Api
MCP_TRANSPORT=http dotnet run
```

The server will start on `http://localhost:7071` by default.

## Running Load Tests

### Using .NET CLI

```bash
dotnet run --project tests/RoslynStone.LoadTests
```

### Custom Configuration

```bash
# Custom base URL, concurrency, and rounds
dotnet run --project tests/RoslynStone.LoadTests -- http://localhost:8080 500 20
```

Arguments:
1. Base URL (default: `http://localhost:7071`)
2. Concurrency (default: `300` requests per round)
3. Rounds (default: `10`)

### Using Cake Build Script

```bash
dotnet cake --target=Load-Test
```

## Test Configuration

Default settings:
- **Concurrency**: 300 concurrent requests per round
- **Rounds**: 10 rounds per scenario
- **Total Requests**: 12,000 (300 × 10 × 4 scenarios)

## Test Scenarios

1. **Simple Expression** - `2 + 2`
2. **Variable Assignment** - `var x = 10; x * 2`
3. **LINQ Query** - `Enumerable.Range(1, 100).Where(x => x % 2 == 0).Sum()`
4. **NuGet Search** - Search for "Newtonsoft.Json"

## Metrics Reported

For each scenario:
- **Average Round Time** - Time to complete all concurrent requests
- **Average Response Time** - Per-request response time
- **Success Rate** - Percentage of successful requests
- **Throughput** - Requests per second
- **Total Success/Failures** - Count of successful and failed requests

## Expected Results

A healthy server should achieve:
- ✅ Success rate > 99%
- ✅ Average response time < 100ms for simple operations
- ✅ Throughput > 1000 requests/second

## Troubleshooting

### Server Not Available

```
❌ Server is not available: Connection refused
```

**Solution**: Start the server first:
```bash
cd src/RoslynStone.Api
MCP_TRANSPORT=http dotnet run
```

### High Failure Rate

If you see a success rate below 99%:
1. Check server logs for errors
2. Verify server has adequate resources
3. Reduce concurrency level
4. Check network connectivity

### Timeout Errors

If requests are timing out:
1. Increase the timeout in `Program.cs`
2. Check server performance
3. Reduce concurrency level

## Performance Tuning

To optimize server performance:
1. Run in Release configuration
2. Use production-ready hosting (Kestrel optimizations)
3. Enable response compression
4. Configure appropriate thread pool sizes
5. Monitor memory usage and GC pressure

## CI Integration

Load tests are optional in CI and can be run manually:
- They require a running server instance
- Results should be monitored for performance regressions
- Consider using dedicated load testing infrastructure
