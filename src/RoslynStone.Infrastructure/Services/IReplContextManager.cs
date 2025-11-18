using Microsoft.CodeAnalysis.Scripting;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Interface for managing REPL context lifecycle and state
/// </summary>
public interface IReplContextManager
{
    /// <summary>
    /// Create a new REPL context and return its unique identifier
    /// </summary>
    /// <returns>A unique context ID (GUID)</returns>
    string CreateContext();

    /// <summary>
    /// Check if a context with the given ID exists
    /// </summary>
    /// <param name="contextId">The context ID to check</param>
    /// <returns>True if the context exists, false otherwise</returns>
    bool ContextExists(string contextId);

    /// <summary>
    /// Get the script state for a given context
    /// </summary>
    /// <param name="contextId">The context ID</param>
    /// <returns>The ScriptState for the context, or null if not found</returns>
    ScriptState? GetContextState(string contextId);

    /// <summary>
    /// Update the script state for a given context
    /// </summary>
    /// <param name="contextId">The context ID</param>
    /// <param name="state">The new script state</param>
    void UpdateContextState(string contextId, ScriptState state);

    /// <summary>
    /// Remove a context and its associated state
    /// </summary>
    /// <param name="contextId">The context ID to remove</param>
    /// <returns>True if the context was removed, false if it didn't exist</returns>
    bool RemoveContext(string contextId);

    /// <summary>
    /// Remove all expired contexts based on the configured timeout
    /// </summary>
    /// <returns>The number of contexts removed</returns>
    int CleanupExpiredContexts();

    /// <summary>
    /// Get all active context IDs
    /// </summary>
    /// <returns>Collection of active context IDs</returns>
    IReadOnlyCollection<string> GetActiveContexts();

    /// <summary>
    /// Get metadata about a specific context
    /// </summary>
    /// <param name="contextId">The context ID</param>
    /// <returns>Context metadata, or null if not found</returns>
    ReplContextMetadata? GetContextMetadata(string contextId);
}

/// <summary>
/// Metadata about a REPL context
/// </summary>
public class ReplContextMetadata
{
    /// <summary>
    /// The unique context identifier
    /// </summary>
    public required string ContextId { get; init; }

    /// <summary>
    /// When the context was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the context was last accessed
    /// </summary>
    public required DateTimeOffset LastAccessedAt { get; init; }

    /// <summary>
    /// Number of executions in this context
    /// </summary>
    public required int ExecutionCount { get; init; }

    /// <summary>
    /// Whether the context has been initialized with code execution
    /// </summary>
    public required bool IsInitialized { get; init; }
}
