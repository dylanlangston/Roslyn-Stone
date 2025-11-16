///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    Information("Running tasks...");
    Information($"Configuration: {configuration}");
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
});

Task("CI")
    .Description("Run all CI tasks: Format check, Build, Inspect, Test")
    .IsDependentOn("Format-Check")
    .IsDependentOn("Inspect")
    .IsDependentOn("Test");

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
