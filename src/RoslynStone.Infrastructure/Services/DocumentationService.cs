using System.Reflection;
using System.Xml.Linq;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using RoslynStone.Core.Models;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for looking up XML documentation for .NET types and members, including NuGet packages
/// </summary>
public class DocumentationService
{
    private readonly Dictionary<string, XDocument> _documentationCache = new();
    private readonly NuGetService? _nugetService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentationService"/> class
    /// </summary>
    /// <param name="nugetService">Optional NuGet service for package documentation lookup</param>
    public DocumentationService(NuGetService? nugetService = null)
    {
        _nugetService = nugetService;
    }

    /// <summary>
    /// Get documentation for a symbol (type, method, property, etc.)
    /// </summary>
    /// <param name="symbolName">The symbol name to look up (e.g., "System.String" or "Newtonsoft.Json.JsonConvert")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Documentation information if found, otherwise null</returns>
    public async Task<DocumentationInfo?> GetDocumentationAsync(
        string symbolName,
        CancellationToken cancellationToken = default
    )
    {
        return await GetDocumentationAsync(symbolName, packageId: null, cancellationToken);
    }

    /// <summary>
    /// Get documentation for a symbol with an optional package hint
    /// </summary>
    /// <param name="symbolName">The symbol name to look up</param>
    /// <param name="packageId">Optional NuGet package ID to search (e.g., "Newtonsoft.Json")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Documentation information if found, otherwise null</returns>
    public async Task<DocumentationInfo?> GetDocumentationAsync(
        string symbolName,
        string? packageId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // First, try the standard approach (loaded assemblies)
            var type = ResolveType(symbolName);
            if (type != null)
            {
                var xmlDoc = GetXmlDocumentation(type.Assembly);
                if (xmlDoc != null)
                {
                    var memberName = GetMemberName(type, symbolName);
                    var memberElement = xmlDoc
                        .Descendants("member")
                        .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

                    if (memberElement != null)
                    {
                        return new DocumentationInfo
                        {
                            SymbolName = symbolName,
                            Summary = GetElementValue(memberElement, "summary"),
                            Remarks = GetElementValue(memberElement, "remarks"),
                            Parameters = GetParameters(memberElement),
                            Returns = GetElementValue(memberElement, "returns"),
                            Exceptions = GetExceptions(memberElement),
                            Example = GetElementValue(memberElement, "example"),
                            FullDocumentation = memberElement.ToString(),
                        };
                    }
                }
            }

            // If not found and packageId is provided, try NuGet package documentation
            if (!string.IsNullOrWhiteSpace(packageId) && _nugetService != null)
            {
                return await GetNuGetPackageDocumentationAsync(
                    symbolName,
                    packageId,
                    cancellationToken
                );
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private Type? ResolveType(string symbolName)
    {
        // Try to find the type in loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetType(symbolName);
                if (type != null)
                    return type;

                // Try searching for types with the name
                var types = assembly
                    .GetTypes()
                    .Where(t => t.FullName?.Contains(symbolName) == true || t.Name == symbolName)
                    .ToList(); // Materialize to avoid multiple enumeration

                if (types.Count > 0)
                    return types[0];
            }
            catch
            {
                // Skip assemblies that can't be loaded
            }
        }

        return null;
    }

    private XDocument? GetXmlDocumentation(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        if (assemblyName == null)
            return null;

        if (_documentationCache.TryGetValue(assemblyName, out var cached))
            return cached;

        try
        {
            var xmlPath = Path.Combine(
                Path.GetDirectoryName(assembly.Location) ?? "",
                $"{assemblyName}.xml"
            );

            if (File.Exists(xmlPath))
            {
                var doc = XDocument.Load(xmlPath);
                _documentationCache[assemblyName] = doc;
                return doc;
            }
        }
        catch
        {
            // XML documentation not available
        }

        return null;
    }

    private string GetMemberName(Type type, string symbolName)
    {
        // For types, the format is T:FullTypeName
        if (symbolName.Contains("("))
        {
            // Method with parameters
            return $"M:{symbolName}";
        }
        else if (symbolName.Contains("."))
        {
            // Could be a property, field, or nested type
            return $"P:{symbolName}";
        }
        else
        {
            return $"T:{type.FullName}";
        }
    }

    private string? GetElementValue(XElement parent, string elementName)
    {
        var element = parent.Element(elementName);
        return element?.Value.Trim();
    }

    private Dictionary<string, string> GetParameters(XElement memberElement)
    {
        var parameters = new Dictionary<string, string>();
        foreach (var param in memberElement.Elements("param"))
        {
            var name = param.Attribute("name")?.Value;
            var description = param.Value.Trim();
            if (name != null)
                parameters[name] = description;
        }
        return parameters;
    }

    private List<string> GetExceptions(XElement memberElement)
    {
        return memberElement
            .Elements("exception")
            .Select(e => $"{e.Attribute("cref")?.Value}: {e.Value.Trim()}")
            .ToList();
    }

    /// <summary>
    /// Get documentation from a NuGet package by downloading it at runtime
    /// </summary>
    private async Task<DocumentationInfo?> GetNuGetPackageDocumentationAsync(
        string symbolName,
        string packageId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var xmlDoc = await GetNuGetPackageXmlDocumentationAsync(packageId, cancellationToken);
            if (xmlDoc == null)
                return null;

            // Try different member name formats
            var possibleMemberNames = new[]
            {
                $"T:{symbolName}", // Type
                $"M:{symbolName}", // Method
                $"P:{symbolName}", // Property
                $"F:{symbolName}", // Field
                $"E:{symbolName}", // Event
            };

            XElement? memberElement = null;
            foreach (var memberName in possibleMemberNames)
            {
                memberElement = xmlDoc
                    .Descendants("member")
                    .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

                if (memberElement != null)
                    break;
            }

            if (memberElement == null)
                return null;

            return new DocumentationInfo
            {
                SymbolName = symbolName,
                Summary = GetElementValue(memberElement, "summary"),
                Remarks = GetElementValue(memberElement, "remarks"),
                Parameters = GetParameters(memberElement),
                Returns = GetElementValue(memberElement, "returns"),
                Exceptions = GetExceptions(memberElement),
                Example = GetElementValue(memberElement, "example"),
                FullDocumentation = memberElement.ToString(),
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get XML documentation file for a NuGet package by downloading it at runtime using NuGet.Protocol
    /// </summary>
    private async Task<XDocument?> GetNuGetPackageXmlDocumentationAsync(
        string packageId,
        CancellationToken cancellationToken
    )
    {
        if (_nugetService == null)
            return null;

        // Check cache first
        var cacheKey = $"nuget:{packageId}";
        if (_documentationCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            // Use NuGet.Protocol to get package metadata and download the package
            var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var cache = new SourceCacheContext();

            var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>(
                cancellationToken
            );

            var metadata = await metadataResource.GetMetadataAsync(
                packageId,
                includePrerelease: true,
                includeUnlisted: false,
                cache,
                NuGet.Common.NullLogger.Instance,
                cancellationToken
            );

            var metadataList = metadata.ToList();
            if (metadataList.Count == 0)
                return null;

            // Get the latest version
            var packageMetadata = metadataList
                .OrderByDescending(m => m.Identity.Version)
                .FirstOrDefault();

            if (packageMetadata == null)
                return null;

            // Download the package
            var downloadResource = await repository.GetResourceAsync<DownloadResource>(
                cancellationToken
            );

            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(
                Settings.LoadDefaultSettings(null)
            );

            using var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                packageMetadata.Identity,
                new PackageDownloadContext(cache),
                globalPackagesFolder,
                NuGet.Common.NullLogger.Instance,
                cancellationToken
            );

            if (downloadResult.Status != DownloadResourceResultStatus.Available)
                return null;

            // Extract and read XML documentation from the package stream
            using var packageStream = downloadResult.PackageStream;
            using var packageReader = new PackageArchiveReader(packageStream);

            // Get all files in the package
            var files = packageReader.GetFiles().ToList();

            // Find XML files in lib directories (skip ref directories)
            var xmlFiles = files
                .Where(f =>
                    f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                    && (
                        f.Contains("/lib/", StringComparison.OrdinalIgnoreCase)
                        || f.Contains("\\lib\\", StringComparison.OrdinalIgnoreCase)
                    )
                    && !f.Contains("/ref/", StringComparison.OrdinalIgnoreCase)
                    && !f.Contains("\\ref\\", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            // If no lib XML files, try any XML file
            if (xmlFiles.Count == 0)
            {
                xmlFiles = files
                    .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (xmlFiles.Count == 0)
                return null;

            // Read the first XML file
            var xmlFile = xmlFiles.First();
            using var xmlStream = packageReader.GetStream(xmlFile);
            var doc = XDocument.Load(xmlStream);

            // Cache the document
            _documentationCache[cacheKey] = doc;

            cache.Dispose();

            return doc;
        }
        catch
        {
            // XML documentation not available
            return null;
        }
    }
}
