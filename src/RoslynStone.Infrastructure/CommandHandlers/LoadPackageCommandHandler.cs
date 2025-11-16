using RoslynStone.Core.Commands;
using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Infrastructure.CommandHandlers;

/// <summary>
/// Handler for loading NuGet packages
/// </summary>
public class LoadPackageCommandHandler : ICommandHandler<LoadPackageCommand, PackageReference>
{
    private readonly RoslynScriptingService _scriptingService;
    private readonly NuGetService _nugetService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadPackageCommandHandler"/> class
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    /// <param name="nugetService">The NuGet service</param>
    public LoadPackageCommandHandler(
        RoslynScriptingService scriptingService,
        NuGetService nugetService
    )
    {
        _scriptingService = scriptingService;
        _nugetService = nugetService;
    }

    /// <summary>
    /// Handles the load package command
    /// </summary>
    /// <param name="command">The command containing the package name and version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The package reference</returns>
    public async Task<PackageReference> HandleAsync(
        LoadPackageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Download the package and get assembly paths
            var assemblyPaths = await _nugetService.DownloadPackageAsync(
                command.PackageName,
                command.Version,
                cancellationToken
            );

            // Add package reference to scripting service
            _scriptingService.AddPackageReference(
                command.PackageName,
                command.Version,
                assemblyPaths
            );

            return new PackageReference
            {
                Name = command.PackageName,
                Version = command.Version,
                IsLoaded = true,
            };
        }
        catch (Exception)
        {
            return new PackageReference
            {
                Name = command.PackageName,
                Version = command.Version,
                IsLoaded = false,
            };
        }
    }
}
