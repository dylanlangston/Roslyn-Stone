using System.Text.RegularExpressions;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for enforcing filesystem access restrictions
/// Prevents arbitrary file system access from user code
/// </summary>
public partial class FileSystemSecurityService
{
    private readonly HashSet<string> _allowedPaths;
    private readonly bool _enableFileSystemRestrictions;

    [GeneratedRegex(@"^\s*(File\.|Directory\.|Path\.|System\.IO\.)", RegexOptions.IgnoreCase)]
    private static partial Regex FileSystemApiPattern();

    [GeneratedRegex(@"File\.Delete", RegexOptions.IgnoreCase)]
    private static partial Regex FileDeletePattern();

    [GeneratedRegex(@"File\.WriteAllText", RegexOptions.IgnoreCase)]
    private static partial Regex FileWriteAllTextPattern();

    [GeneratedRegex(@"File\.WriteAllBytes", RegexOptions.IgnoreCase)]
    private static partial Regex FileWriteAllBytesPattern();

    [GeneratedRegex(@"Directory\.Delete", RegexOptions.IgnoreCase)]
    private static partial Regex DirectoryDeletePattern();

    [GeneratedRegex(@"Directory\.Create", RegexOptions.IgnoreCase)]
    private static partial Regex DirectoryCreatePattern();

    [GeneratedRegex(@"new\s+FileStream", RegexOptions.IgnoreCase)]
    private static partial Regex FileStreamPattern();

    [GeneratedRegex(@"new\s+StreamWriter", RegexOptions.IgnoreCase)]
    private static partial Regex StreamWriterPattern();

    [GeneratedRegex(@"Process\.Start", RegexOptions.IgnoreCase)]
    private static partial Regex ProcessStartPattern();

    [GeneratedRegex(@"ProcessStartInfo", RegexOptions.IgnoreCase)]
    private static partial Regex ProcessStartInfoPattern();

    /// <summary>
    /// Initializes a new instance of the FileSystemSecurityService
    /// </summary>
    /// <param name="enableFileSystemRestrictions">Whether to enable filesystem restrictions</param>
    /// <param name="allowedPaths">Optional list of allowed filesystem paths</param>
    public FileSystemSecurityService(
        bool enableFileSystemRestrictions = true,
        IEnumerable<string>? allowedPaths = null
    )
    {
        _enableFileSystemRestrictions = enableFileSystemRestrictions;
        _allowedPaths = allowedPaths?.Select(Path.GetFullPath).ToHashSet() ?? new HashSet<string>();
    }

    /// <summary>
    /// Validates if code contains potentially dangerous filesystem operations
    /// </summary>
    public ValidationResult ValidateCode(string code)
    {
        if (!_enableFileSystemRestrictions)
        {
            return ValidationResult.Success();
        }

        var issues = new List<string>();

        // Check for filesystem API usage
        if (FileSystemApiPattern().IsMatch(code))
        {
            issues.Add(
                "File system access is restricted. Code contains File, Directory, Path, or System.IO operations."
            );
        }

        // Check for specific dangerous patterns using generated regex
        var dangerousChecks = new[]
        {
            (FileDeletePattern(), "File.Delete is not allowed"),
            (FileWriteAllTextPattern(), "File.WriteAllText is not allowed"),
            (FileWriteAllBytesPattern(), "File.WriteAllBytes is not allowed"),
            (DirectoryDeletePattern(), "Directory.Delete is not allowed"),
            (DirectoryCreatePattern(), "Directory.Create is not allowed"),
            (FileStreamPattern(), "FileStream creation is not allowed"),
            (StreamWriterPattern(), "StreamWriter creation is not allowed"),
            (ProcessStartPattern(), "Process.Start is not allowed"),
            (ProcessStartInfoPattern(), "ProcessStartInfo is not allowed"),
        };

        foreach (var (regex, message) in dangerousChecks)
        {
            if (regex.IsMatch(code))
            {
                issues.Add(message);
            }
        }

        return issues.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(issues);
    }

    /// <summary>
    /// Validates if a file path is allowed to be accessed
    /// </summary>
    public bool IsPathAllowed(string path)
    {
        if (!_enableFileSystemRestrictions)
        {
            return true;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            return _allowedPaths.Any(allowed =>
                fullPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
            );
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Result of filesystem security validation
/// </summary>
public record ValidationResult(bool IsValid, IReadOnlyList<string> Issues)
{
    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a failed validation result with issues
    /// </summary>
    public static ValidationResult Failure(IEnumerable<string> issues) =>
        new(false, issues.ToList());
}
