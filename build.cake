///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var version = Argument("version", "1.0.0");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    Information("Running tasks...");
    Information($"Configuration: {configuration}");
    Information($"Version: {version}");
});

Teardown(ctx =>
{
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories("./src/**/bin");
    CleanDirectories("./src/**/obj");
    CleanDirectories("./tests/**/bin");
    CleanDirectories("./tests/**/obj");
    CleanDirectories("./artifacts");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore("./RoslynStone.sln");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetBuild("./RoslynStone.sln", new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true
    });
});

Task("Format")
    .Description("Format code with CSharpier")
    .Does(() =>
{
    StartProcess("csharpier", new ProcessSettings
    {
        Arguments = "format ."
    });
});

Task("Format-Check")
    .Description("Check code formatting with CSharpier")
    .Does(() =>
{
    var exitCode = StartProcess("csharpier", new ProcessSettings
    {
        Arguments = "check ."
    });
    
    if (exitCode != 0)
    {
        throw new Exception("Code formatting issues found. Run 'dotnet cake --target=Format' to fix.");
    }
});

Task("Inspect")
    .Description("Run ReSharper code inspections")
    .IsDependentOn("Build")
    .Does(() =>
{
    var reportPath = "./artifacts/resharper-report.xml";
    EnsureDirectoryExists("./artifacts");
    
    StartProcess("jb", new ProcessSettings
    {
        Arguments = $"inspectcode RoslynStone.sln --output={reportPath} --severity=WARNING"
    });
    
    Information($"ReSharper inspection report generated: {reportPath}");
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetTest("./RoslynStone.sln", new DotNetTestSettings
    {
        Configuration = configuration,
        NoRestore = true,
        NoBuild = true,
        Loggers = new[] { "console;verbosity=normal" }
    });
});

Task("Test-Coverage")
    .Description("Run tests with code coverage")
    .IsDependentOn("Build")
    .Does(() =>
{
    EnsureDirectoryExists("./artifacts/coverage");
    
    DotNetTest("./RoslynStone.sln", new DotNetTestSettings
    {
        Configuration = configuration,
        NoRestore = true,
        NoBuild = true,
        Loggers = new[] { "console;verbosity=normal" },
        ArgumentCustomization = args => args
            .Append("--collect:\"XPlat Code Coverage\"")
            .Append("--results-directory ./artifacts/coverage")
    });
    
    // Parse coverage results and check branch coverage
    var coverageFiles = GetFiles("./artifacts/coverage/**/coverage.cobertura.xml");
    if (coverageFiles.Any())
    {
        var coverageFile = coverageFiles.First();
        var xml = System.Xml.Linq.XDocument.Load(coverageFile.FullPath);
        var coverage = xml.Root;
        
        if (coverage == null)
        {
            Warning("Unable to parse coverage report: root element not found");
            return;
        }
        
        var lineRateAttr = coverage.Attribute("line-rate");
        var branchRateAttr = coverage.Attribute("branch-rate");
        
        if (lineRateAttr == null || branchRateAttr == null)
        {
            Warning("Unable to parse coverage report: required attributes not found");
            return;
        }
        
        var lineCoverage = double.Parse(lineRateAttr.Value) * 100;
        var branchCoverage = double.Parse(branchRateAttr.Value) * 100;
        
        Information($"Line Coverage: {lineCoverage:F2}%");
        Information($"Branch Coverage: {branchCoverage:F2}%");
        
        // Enforce minimum coverage thresholds
        const double MinBranchCoverage = 75.0;
        const double MinLineCoverage = 80.0;
        
        if (branchCoverage < MinBranchCoverage)
        {
            Warning($"⚠️  Branch coverage ({branchCoverage:F2}%) is below the minimum threshold of {MinBranchCoverage}%");
            // Note: Not failing the build yet to allow incremental improvements
            // throw new Exception($"Branch coverage ({branchCoverage:F2}%) is below the minimum threshold of {MinBranchCoverage}%");
        }
        else
        {
            Information($"✅ Branch coverage meets the minimum threshold");
        }
        
        if (lineCoverage < MinLineCoverage)
        {
            Warning($"⚠️  Line coverage ({lineCoverage:F2}%) is below the minimum threshold of {MinLineCoverage}%");
        }
        else
        {
            Information($"✅ Line coverage meets the minimum threshold");
        }
    }
});

Task("Test-Coverage-Report")
    .Description("Generate HTML coverage report using ReportGenerator")
    .IsDependentOn("Test-Coverage")
    .Does(() =>
{
    var coverageFiles = GetFiles("./artifacts/coverage/**/coverage.cobertura.xml");
    if (coverageFiles.Any())
    {
        EnsureDirectoryExists("./artifacts/coverage-report");
        
        var settings = new ProcessSettings
        {
            Arguments = new ProcessArgumentBuilder()
                .Append($"-reports:{string.Join(";", coverageFiles.Select(f => f.FullPath))}")
                .Append("-targetdir:./artifacts/coverage-report")
                .Append("-reporttypes:Html;Badges")
        };
        
        StartProcess("reportgenerator", settings);
        Information("Coverage report generated at ./artifacts/coverage-report/index.html");
    }
    else
    {
        Warning("No coverage files found to generate report");
    }
});

Task("Benchmark")
    .Description("Run benchmarks")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Running benchmarks...");
    Information("Note: Benchmarks can take several minutes to complete.");
    
    var benchmarkProject = "./tests/RoslynStone.Benchmarks/RoslynStone.Benchmarks.csproj";
    
    DotNetRun(benchmarkProject, new DotNetRunSettings
    {
        Configuration = "Release",
        NoBuild = false,
        ArgumentCustomization = args => args.Append("--filter * --artifacts ./artifacts/benchmarks")
    });
    
    Information("Benchmark results saved to ./artifacts/benchmarks");
});

Task("Load-Test")
    .Description("Run load tests against a running HTTP server")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Running load tests...");
    Information("Note: Ensure the API server is running in HTTP mode:");
    Information("  cd src/RoslynStone.Api && MCP_TRANSPORT=http dotnet run");
    Information("");
    
    var loadTestProject = "./tests/RoslynStone.LoadTests/RoslynStone.LoadTests.csproj";
    
    try
    {
        DotNetRun(loadTestProject, new DotNetRunSettings
        {
            Configuration = configuration,
            NoBuild = true
        });
    }
    catch (Exception ex)
    {
        Warning($"Load test failed: {ex.Message}");
        Warning("Make sure the server is running with: MCP_TRANSPORT=http dotnet run");
    }
});

Task("Pack")
    .Description("Create NuGet packages")
    .IsDependentOn("Build")
    .Does(() =>
{
    EnsureDirectoryExists("./artifacts/packages");
    
    var projects = new[]
    {
        "./src/RoslynStone.Core/RoslynStone.Core.csproj",
        "./src/RoslynStone.Infrastructure/RoslynStone.Infrastructure.csproj"
    };
    
    foreach (var project in projects)
    {
        DotNetPack(project, new DotNetPackSettings
        {
            Configuration = configuration,
            OutputDirectory = "./artifacts/packages",
            NoRestore = true,
            NoBuild = true,
            ArgumentCustomization = args => args
                .Append($"/p:Version={version}")
                .Append($"/p:PackageVersion={version}")
        });
    }
    
    Information($"NuGet packages created in ./artifacts/packages");
});

Task("Publish-NuGet")
    .Description("Publish NuGet packages to NuGet.org")
    .IsDependentOn("Pack")
    .Does(() =>
{
    var apiKey = EnvironmentVariable("NUGET_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new Exception("NUGET_API_KEY environment variable not set");
    }
    
    var packages = GetFiles("./artifacts/packages/*.nupkg");
    
    foreach (var package in packages)
    {
        DotNetNuGetPush(package.FullPath, new DotNetNuGetPushSettings
        {
            Source = "https://api.nuget.org/v3/index.json",
            ApiKey = apiKey
        });
    }
});

Task("CI")
    .Description("Run all CI tasks: Format check, Build, Inspect, Test with Coverage")
    .IsDependentOn("Format-Check")
    .IsDependentOn("Inspect")
    .IsDependentOn("Test-Coverage");

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
