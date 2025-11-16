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

    public LoadPackageCommandHandler(RoslynScriptingService scriptingService)
    {
        _scriptingService = scriptingService;
    }

    public Task<PackageReference> HandleAsync(LoadPackageCommand command, CancellationToken cancellationToken = default)
    {
        _scriptingService.AddPackageReference(command.PackageName, command.Version);

        return Task.FromResult(new PackageReference
        {
            Name = command.PackageName,
            Version = command.Version,
            IsLoaded = true
        });
    }
}
