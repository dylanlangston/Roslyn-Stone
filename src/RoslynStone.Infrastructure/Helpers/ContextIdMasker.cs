namespace RoslynStone.Infrastructure.Helpers;

/// <summary>
/// Helper class for masking context IDs in logs to prevent session exposure
/// </summary>
public static class ContextIdMasker
{
    /// <summary>
    /// Masks a context ID for safe logging
    /// Shows first 8 characters for debugging, masks the rest
    /// </summary>
    /// <param name="contextId">The context ID to mask</param>
    /// <param name="shouldMask">Whether masking is enabled</param>
    /// <returns>Masked context ID or original if masking disabled</returns>
    public static string Mask(string? contextId, bool shouldMask)
    {
        if (string.IsNullOrWhiteSpace(contextId))
            return "[null]";

        if (!shouldMask)
            return contextId;

        // Show first 8 chars for debugging, mask the rest
        if (contextId.Length <= 8)
            return new string('*', contextId.Length);

        return $"{contextId[..8]}...{new string('*', 4)}";
    }

    /// <summary>
    /// Masks a context ID for safe logging with explicit "MASKED:" prefix
    /// </summary>
    public static string MaskWithPrefix(string? contextId, bool shouldMask)
    {
        var masked = Mask(contextId, shouldMask);
        return shouldMask && !string.IsNullOrWhiteSpace(contextId) ? $"MASKED:{masked}" : masked;
    }
}
