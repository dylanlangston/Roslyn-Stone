using System.Reflection;
using System.Xml.Linq;
using NuGet.Configuration;
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
    /// <returns>Documentation information if found, otherwise null</returns>
    public DocumentationInfo? GetDocumentation(string symbolName)
    {
        return GetDocumentation(symbolName, packageId: null);
    }

    /// <summary>
    /// Get documentation for a symbol with an optional package hint
    /// </summary>
    /// <param name="symbolName">The symbol name to look up</param>
    /// <param name="packageId">Optional NuGet package ID to search (e.g., "Newtonsoft.Json")</param>
    /// <returns>Documentation information if found, otherwise null</returns>
    public DocumentationInfo? GetDocumentation(string symbolName, string? packageId)
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
                return GetNuGetPackageDocumentation(symbolName, packageId);
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
    /// Get documentation from a NuGet package's XML file
    /// </summary>
    private DocumentationInfo? GetNuGetPackageDocumentation(string symbolName, string packageId)
    {
        try
        {
            var xmlDoc = GetNuGetPackageXmlDocumentation(packageId);
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
    /// Get XML documentation file for a NuGet package
    /// </summary>
    private XDocument? GetNuGetPackageXmlDocumentation(string packageId)
    {
        // Check cache first
        var cacheKey = $"nuget:{packageId}";
        if (_documentationCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            // Get NuGet global packages folder
            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(
                Settings.LoadDefaultSettings(null)
            );

            // NuGet stores packages in lowercase
            var packagePath = Path.Combine(globalPackagesFolder, packageId.ToLowerInvariant());

            if (!Directory.Exists(packagePath))
                return null;

            // Find the latest version directory
            var versionDirs = Directory
                .GetDirectories(packagePath)
                .OrderByDescending(d => d)
                .ToList();

            if (versionDirs.Count == 0)
                return null;

            // Search for XML documentation files in the package
            foreach (var versionDir in versionDirs)
            {
                var xmlFiles = Directory.GetFiles(versionDir, "*.xml", SearchOption.AllDirectories);

                // Prefer XML files in lib directories and skip ref directories
                var libXmlFiles = xmlFiles
                    .Where(f =>
                        f.Contains("/lib/", StringComparison.OrdinalIgnoreCase)
                        || f.Contains("\\lib\\", StringComparison.OrdinalIgnoreCase)
                    )
                    .Where(f =>
                        !f.Contains("/ref/", StringComparison.OrdinalIgnoreCase)
                        && !f.Contains("\\ref\\", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                var xmlFile = libXmlFiles.FirstOrDefault() ?? xmlFiles.FirstOrDefault();

                if (xmlFile != null && File.Exists(xmlFile))
                {
                    var doc = XDocument.Load(xmlFile);
                    _documentationCache[cacheKey] = doc;
                    return doc;
                }
            }
        }
        catch
        {
            // XML documentation not available
        }

        return null;
    }
}
