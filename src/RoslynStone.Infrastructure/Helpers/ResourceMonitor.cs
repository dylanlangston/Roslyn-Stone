using System.Diagnostics;

namespace RoslynStone.Infrastructure.Helpers;

/// <summary>
/// Helper class for monitoring resource usage and enforcing limits
/// </summary>
public static class ResourceMonitor
{
    /// <summary>
    /// Check if current memory usage exceeds the specified limit
    /// </summary>
    /// <param name="maxMemoryBytes">Maximum allowed memory in bytes (0 = no limit)</param>
    /// <returns>True if memory limit is exceeded, false otherwise</returns>
    public static bool IsMemoryLimitExceeded(long maxMemoryBytes)
    {
        if (maxMemoryBytes <= 0)
            return false;

        var currentProcess = Process.GetCurrentProcess();
        var workingSet = currentProcess.WorkingSet64;

        return workingSet > maxMemoryBytes;
    }

    /// <summary>
    /// Get current memory usage in bytes
    /// </summary>
    /// <returns>Current working set size in bytes</returns>
    public static long GetCurrentMemoryUsage()
    {
        var currentProcess = Process.GetCurrentProcess();
        return currentProcess.WorkingSet64;
    }

    /// <summary>
    /// Get current memory usage in megabytes
    /// </summary>
    /// <returns>Current working set size in MB</returns>
    public static double GetCurrentMemoryUsageMb()
    {
        return GetCurrentMemoryUsage() / (1024.0 * 1024.0);
    }

    /// <summary>
    /// Throw an exception if memory limit is exceeded
    /// </summary>
    /// <param name="maxMemoryBytes">Maximum allowed memory in bytes (0 = no limit)</param>
    /// <exception cref="InsufficientMemoryException">Thrown when memory limit is exceeded</exception>
    public static void ThrowIfMemoryLimitExceeded(long maxMemoryBytes)
    {
        if (IsMemoryLimitExceeded(maxMemoryBytes))
        {
            var currentMb = GetCurrentMemoryUsageMb();
            var limitMb = maxMemoryBytes / (1024.0 * 1024.0);
            throw new InsufficientMemoryException(
                $"Memory limit exceeded: Current usage {currentMb:F2} MB, Limit {limitMb:F2} MB"
            );
        }
    }

    /// <summary>
    /// Create a memory monitoring task that periodically checks memory usage
    /// </summary>
    /// <param name="maxMemoryBytes">Maximum allowed memory in bytes</param>
    /// <param name="checkIntervalMs">Check interval in milliseconds (default: 100ms)</param>
    /// <param name="cancellationToken">Cancellation token to stop monitoring</param>
    /// <returns>Task that completes when monitoring is cancelled or memory limit is exceeded</returns>
    public static async Task MonitorMemoryAsync(
        long maxMemoryBytes,
        int checkIntervalMs = 100,
        CancellationToken cancellationToken = default
    )
    {
        if (maxMemoryBytes <= 0)
            return;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ThrowIfMemoryLimitExceeded(maxMemoryBytes);
                await Task.Delay(checkIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation, don't propagate
        }
    }
}
