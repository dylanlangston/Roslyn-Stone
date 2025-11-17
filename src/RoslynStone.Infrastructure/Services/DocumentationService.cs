using System.Reflection;
using System.Xml.Linq;
using RoslynStone.Core.Models;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for looking up XML documentation for .NET types and members
/// </summary>
public class DocumentationService
{
    private readonly Dictionary<string, XDocument> _documentationCache = new();

    /// <summary>
    /// Get documentation for a symbol (type, method, property, etc.)
    /// </summary>
    public DocumentationInfo? GetDocumentation(string symbolName)
    {
        try
        {
            // Try to resolve the type
            var type = ResolveType(symbolName);
            if (type == null)
                return null;

            // Load XML documentation
            var xmlDoc = GetXmlDocumentation(type.Assembly);
            if (xmlDoc == null)
                return null;

            // Extract documentation
            var memberName = GetMemberName(type, symbolName);
            var memberElement = xmlDoc
                .Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

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
}
