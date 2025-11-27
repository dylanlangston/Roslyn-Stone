namespace RoslynStone.Infrastructure.Models;

/// <summary>
/// Configuration for security settings and resource limits
/// </summary>
public class SecurityConfiguration
{
    /// <summary>
    /// Maximum execution time for code execution
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum memory usage in bytes (0 = no limit)
    /// Default: 512 MB
    /// </summary>
    public long MaxMemoryBytes { get; set; } = 512 * 1024 * 1024; // 512 MB

    /// <summary>
    /// Whether to enforce execution timeouts
    /// Default: true
    /// </summary>
    public bool EnableExecutionTimeout { get; set; } = true;

    /// <summary>
    /// Whether to enforce memory limits
    /// Default: true
    /// </summary>
    public bool EnableMemoryLimits { get; set; } = true;

    /// <summary>
    /// Whether to perform static analysis to block dangerous APIs (filesystem, network, etc.)
    /// Default: true
    /// Note: This is defense-in-depth. Container-level restrictions (seccomp, AppArmor) provide stronger isolation.
    /// </summary>
    public bool EnableApiRestrictions { get; set; } = true;

    /// <summary>
    /// Whether to restrict filesystem access
    /// Default: true
    /// </summary>
    public bool EnableFilesystemRestrictions { get; set; } = true;

    /// <summary>
    /// Allowed filesystem paths for read/write operations
    /// Empty list means no restrictions (not recommended for production)
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global - Updated by configuration
    public List<string> AllowedFilesystemPaths { get; set; } = new();

    /// <summary>
    /// Blocked filesystem paths (takes precedence over allowed paths)
    /// Default: System directories
    /// </summary>
    public List<string> BlockedFilesystemPaths { get; set; } =
        new()
        {
            "/bin",
            "/boot",
            "/dev",
            "/etc",
            "/lib",
            "/lib64",
            "/proc",
            "/root",
            "/sbin",
            "/sys",
            "/usr/bin",
            "/usr/sbin",
            @"C:\Windows",
            @"C:\Program Files",
            @"C:\Program Files (x86)",
        };

    /// <summary>
    /// Whether to log context IDs (security risk if enabled)
    /// Default: false
    /// </summary>
    public bool LogContextIds { get; set; }

    /// <summary>
    /// Blocked assemblies for AssemblyLoadContext (dangerous APIs)
    /// Prevents loading of assemblies that could be used maliciously
    /// Empty list means no restrictions (not recommended for production)
    /// </summary>
    public List<string> BlockedAssemblies { get; set; } = new();

    /// <summary>
    /// Creates a default production-ready security configuration
    /// </summary>
    public static SecurityConfiguration CreateProductionDefaults() =>
        new()
        {
            ExecutionTimeout = TimeSpan.FromSeconds(30),
            MaxMemoryBytes = 512 * 1024 * 1024, // 512 MB
            EnableExecutionTimeout = true,
            EnableMemoryLimits = true,
            EnableFilesystemRestrictions = true,
            LogContextIds = false,
            AllowedFilesystemPaths = new List<string>(),
            BlockedFilesystemPaths = new List<string>
            {
                "/bin",
                "/boot",
                "/dev",
                "/etc",
                "/lib",
                "/lib64",
                "/proc",
                "/root",
                "/sbin",
                "/sys",
                "/usr/bin",
                "/usr/sbin",
                @"C:\Windows",
                @"C:\Program Files",
                @"C:\Program Files (x86)",
            },
            BlockedAssemblies = new List<string>
            {
                // Native code interop - could be used for privilege escalation
                "System.Runtime.InteropServices",
                "System.Runtime.InteropServices.RuntimeInformation",
                // Unsafe code - could be used for memory manipulation
                "System.Runtime.CompilerServices.Unsafe",
                // Reflection emit - could be used to generate malicious code
                "System.Reflection.Emit",
                // Code DOM - alternative code generation
                "System.CodeDom.Compiler",
                "Microsoft.CSharp",
            },
        };

    /// <summary>
    /// Creates a permissive configuration for development/testing
    /// WARNING: Less restrictive than production, but still enforces core security
    /// </summary>
    public static SecurityConfiguration CreateDevelopmentDefaults() =>
        new()
        {
            ExecutionTimeout = TimeSpan.FromMinutes(5),
            MaxMemoryBytes = 100 * 1024 * 1024, // 100MB
            EnableExecutionTimeout = true, // CHANGED: Always enforce timeout
            EnableMemoryLimits = true, // CHANGED: Always enforce memory limits
            EnableFilesystemRestrictions = true, // CHANGED: Always restrict filesystem
            LogContextIds = true, // Dev only - log actual IDs for debugging
            AllowedFilesystemPaths = new List<string>(), // Empty = allow all
            BlockedFilesystemPaths = new List<string>(), // Empty = no blocks
            BlockedAssemblies = new List<string>(), // No restrictions in dev
        };
}
