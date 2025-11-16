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

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadPackageCommandHandler"/> class
    /// </summary>
    /// <param name="scriptingService">The Roslyn scripting service</param>
    public LoadPackageCommandHandler(RoslynScriptingService scriptingService)
    {
        _scriptingService = scriptingService;
    }

    /// <summary>
    /// Handles the load package command
    /// </summary>
    /// <param name="command">The command containing the package name and version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The package reference</returns>
    public Task<PackageReference> HandleAsync(
        LoadPackageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        // Note: AddPackageReference is currently a stub - actual NuGet loading not implemented
        _scriptingService.AddPackageReference(command.PackageName, command.Version);

        return Task.FromResult(
            new PackageReference
            {
                Name = command.PackageName,
                Version = command.Version,
                IsLoaded = false, // TODO: Implement actual NuGet package loading
            }
        );
    }
}
