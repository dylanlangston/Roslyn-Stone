using System.Reflection;
using System.Xml.Linq;
using RoslynStone.Core.Models;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for looking up XML documentation for .NET types and members, including NuGet packages
/// </summary>
public class DocumentationService
{
    private readonly Dictionary<string, XDocument> _documentationCache = new();
    private readonly NuGetService? _nugetService;
    private readonly List<string> _dotnetRefAssemblyPaths = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentationService"/> class
    /// </summary>
    /// <param name="nugetService">Optional NuGet service for package documentation lookup</param>
    public DocumentationService(NuGetService? nugetService = null)
    {
        _nugetService = nugetService;
        InitializeDotnetRefAssemblyPaths();
    }

    /// <summary>
    /// Initialize paths to .NET SDK reference assembly XML documentation
    /// </summary>
    private void InitializeDotnetRefAssemblyPaths()
    {
        try
        {
            // Common locations for .NET reference assemblies across platforms
            var possibleBasePaths = new[]
            {
                "/usr/share/dotnet/packs", // Linux
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "dotnet",
                    "packs"
                ), // Windows
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "dotnet",
                    "packs"
                ), // Windows x86
                "/usr/local/share/dotnet/packs", // macOS
            };

            var refAssemblyPaths = possibleBasePaths
                .Where(Directory.Exists)
                .Select(basePath => Path.Combine(basePath, "Microsoft.NETCore.App.Ref"))
                .Where(Directory.Exists)
                .SelectMany(GetRefAssemblyPathsFromPack)
                .ToList();

            _dotnetRefAssemblyPaths.AddRange(refAssemblyPaths);
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // If initialization fails, we'll fall back to standard assembly location lookup
            // Gracefully degrade rather than failing the entire service
        }
    }

    /// <summary>
    /// Get reference assembly paths from a .NET pack directory
    /// </summary>
    private static IEnumerable<string> GetRefAssemblyPathsFromPack(string packPath)
    {
        try
        {
            // Get all version directories, sorted descending (newest first)
            return Directory
                .GetDirectories(packPath)
                .OrderByDescending(d => d)
                .SelectMany(versionDir =>
                {
                    var refDir = Path.Combine(versionDir, "ref");
                    return Directory.Exists(refDir)
                        ? Directory
                            .GetDirectories(refDir)
                            .OrderByDescending(d => d)
                            .Where(tfmDir => Directory.GetFiles(tfmDir, "*.xml").Length > 0)
                        : [];
                });
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return [];
        }
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
        ArgumentNullException.ThrowIfNull(symbolName);
        if (string.IsNullOrWhiteSpace(symbolName))
            return null;

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
        ArgumentNullException.ThrowIfNull(symbolName);
        if (string.IsNullOrWhiteSpace(symbolName))
            return null;

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
                        return CreateDocumentationInfo(memberElement, symbolName);
                    }
                }
            }

            // If not found in loaded assemblies, search .NET SDK reference assemblies
            var sdkDocumentation = SearchDotnetRefAssemblies(symbolName);
            if (sdkDocumentation != null)
            {
                return sdkDocumentation;
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Gracefully degrade on any unexpected errors
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
                    .Where(t =>
                        t.FullName?.Contains(symbolName, StringComparison.Ordinal) == true
                        || t.Name == symbolName
                    )
                    .ToList(); // Materialize to avoid multiple enumeration

                if (types.Count > 0)
                    return types[0];
            }
            catch (Exception ex)
                when (ex
                        is ReflectionTypeLoadException
                            or FileNotFoundException
                            or FileLoadException
                            or BadImageFormatException
                )
            {
                // Skip assemblies that can't be loaded or inspected
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
            var assemblyLocation = assembly.Location;
            if (string.IsNullOrEmpty(assemblyLocation))
                return null;

            var xmlPath = Path.Combine(
                Path.GetDirectoryName(assemblyLocation) ?? "",
                $"{assemblyName}.xml"
            );

            if (File.Exists(xmlPath))
            {
                var doc = XDocument.Load(xmlPath);
                _documentationCache[assemblyName] = doc;
                return doc;
            }
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            // XML documentation not available or can't be loaded
        }

        return null;
    }

    /// <summary>
    /// Search .NET SDK reference assemblies for documentation
    /// </summary>
    private DocumentationInfo? SearchDotnetRefAssemblies(string symbolName)
    {
        if (_dotnetRefAssemblyPaths.Count == 0)
            return null;

        try
        {
            // Try different member name formats
            var possibleMemberNames = new[]
            {
                $"T:{symbolName}", // Type
                $"M:{symbolName}", // Method
                $"P:{symbolName}", // Property
                $"F:{symbolName}", // Field
                $"E:{symbolName}", // Event
            };

            var docInfo = _dotnetRefAssemblyPaths
                .SelectMany(GetXmlFilesFromDirectory)
                .Select(xmlFile => (xmlFile, xmlDoc: LoadOrGetCachedXmlDocument(xmlFile, $"sdk:{xmlFile}")))
                .Where(tuple => tuple.xmlDoc != null)
                .Select(tuple => FindMemberDocumentation(tuple.xmlDoc!, possibleMemberNames, symbolName))
                .FirstOrDefault(doc => doc != null);

            if (docInfo != null)
                return docInfo;

            return null;
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Gracefully degrade on file system errors
            return null;
        }
    }

    /// <summary>
    /// Get all XML files from a directory, handling I/O errors gracefully
    /// </summary>
    private static string[] GetXmlFilesFromDirectory(string directory)
    {
        try
        {
            return Directory.GetFiles(directory, "*.xml");
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return [];
        }
    }

    /// <summary>
    /// Load XML document from file or get from cache
    /// </summary>
    private XDocument? LoadOrGetCachedXmlDocument(string xmlFile, string cacheKey)
    {
        if (_documentationCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var xmlDoc = XDocument.Load(xmlFile);
            _documentationCache[cacheKey] = xmlDoc;
            return xmlDoc;
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            // Skip files that can't be loaded
            return null;
        }
    }

    /// <summary>
    /// Find documentation for a member in an XML document
    /// </summary>
    private DocumentationInfo? FindMemberDocumentation(
        XDocument xmlDoc,
        string[] possibleMemberNames,
        string symbolName
    )
    {
        foreach (var memberName in possibleMemberNames)
        {
            var memberElement = xmlDoc
                .Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

            if (memberElement != null)
            {
                return CreateDocumentationInfo(memberElement, symbolName);
            }
        }

        return null;
    }

    /// <summary>
    /// Create a DocumentationInfo object from an XML member element
    /// </summary>
    private DocumentationInfo CreateDocumentationInfo(XElement memberElement, string symbolName)
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

            return FindMemberDocumentation(xmlDoc, possibleMemberNames, symbolName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log and swallow exceptions for graceful degradation
            // TODO: Add proper logging when logger is available
            return null;
        }
    }

    /// <summary>
    /// Get XML documentation file for a NuGet package by downloading it at runtime using NuGet.Protocol.
    /// Downloads the latest version of the package from NuGet.org and caches the result per package.
    /// </summary>
    /// <param name="packageId">The NuGet package ID to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The XDocument containing XML documentation if found; otherwise null.
    /// Returns null if: package not found, package has no XML documentation files, symbol not found in docs, or any error occurs.
    /// </returns>
    private async Task<XDocument?> GetNuGetPackageXmlDocumentationAsync(
        string packageId,
        CancellationToken cancellationToken
    )
    {
        if (_nugetService == null)
            return null;

        // Validate packageId to prevent security issues
        if (
            string.IsNullOrWhiteSpace(packageId)
            || packageId.Contains(':')
            || packageId.Contains('/')
            || packageId.Contains('\\')
        )
            return null;

        // Check cache first
        var cacheKey = $"nuget:{packageId}";
        if (_documentationCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            // Use NuGetService to download the package
            using var packageResult = await _nugetService.GetPackageReaderAsync(
                packageId,
                cancellationToken
            );

            if (packageResult == null)
                return null;

            var packageReader = packageResult.PackageReader;

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

            return doc;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log and swallow exceptions for graceful degradation
            // TODO: Add proper logging when logger is available
            return null;
        }
    }
}
