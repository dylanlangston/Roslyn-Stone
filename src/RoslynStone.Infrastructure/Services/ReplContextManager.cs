using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Manages REPL context lifecycle and state
/// Thread-safe implementation using concurrent collections
/// </summary>
public class ReplContextManager : IReplContextManager
{
    private readonly ConcurrentDictionary<string, ReplContext> _contexts = new();
    private readonly TimeSpan _contextTimeout;
    private readonly ILogger<ReplContextManager>? _logger;

    /// <summary>
    /// Internal class to track context state and metadata
    /// Thread-safe with Lock object for property updates
    /// </summary>
    private class ReplContext
    {
        private readonly Lock _lock = new();

        public string ContextId { get; init; } = string.Empty;
        public ScriptState? State { get; set; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset LastAccessedAt { get; set; }
        public int ExecutionCount { get; set; }
        public bool IsInitialized => State != null;

        /// <summary>
        /// Thread-safe update of LastAccessedAt
        /// </summary>
        public void UpdateLastAccessed()
        {
            lock (_lock)
            {
                LastAccessedAt = DateTimeOffset.UtcNow;
            }
        }

        /// <summary>
        /// Thread-safe increment of ExecutionCount
        /// </summary>
        public void IncrementExecutionCount()
        {
            lock (_lock)
            {
                ExecutionCount++;
            }
        }

        /// <summary>
        /// Thread-safe update of State with LastAccessedAt
        /// </summary>
        public void UpdateState(ScriptState state)
        {
            lock (_lock)
            {
                State = state;
                LastAccessedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the ReplContextManager
    /// </summary>
    /// <param name="contextTimeout">Timeout for context expiration (default: 30 minutes)</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public ReplContextManager(
        TimeSpan? contextTimeout = null,
        ILogger<ReplContextManager>? logger = null
    )
    {
        _contextTimeout = contextTimeout ?? TimeSpan.FromMinutes(30);
        _logger = logger;
    }

    /// <inheritdoc/>
    public string CreateContext()
    {
        var contextId = Guid.NewGuid().ToString();
        var context = new ReplContext
        {
            ContextId = contextId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastAccessedAt = DateTimeOffset.UtcNow,
            ExecutionCount = 0,
        };

        if (_contexts.TryAdd(contextId, context))
        {
            _logger?.LogDebug("Created new REPL context: {ContextId}", contextId);
            return contextId;
        }

        // Very unlikely but handle collision by retrying
        _logger?.LogWarning("Context ID collision detected, retrying");
        return CreateContext();
    }

    /// <inheritdoc/>
    public bool ContextExists(string contextId)
    {
        if (string.IsNullOrWhiteSpace(contextId))
            return false;

        return _contexts.ContainsKey(contextId);
    }

    /// <inheritdoc/>
    public ScriptState? GetContextState(string contextId)
    {
        if (string.IsNullOrWhiteSpace(contextId))
            return null;

        if (_contexts.TryGetValue(contextId, out var context))
        {
            // Thread-safe update of last accessed time
            context.UpdateLastAccessed();
            return context.State;
        }

        return null;
    }

    /// <inheritdoc/>
    public void UpdateContextState(string contextId, ScriptState state)
    {
        if (string.IsNullOrWhiteSpace(contextId))
            throw new ArgumentException("Context ID cannot be null or empty", nameof(contextId));

        if (!_contexts.TryGetValue(contextId, out var context))
            throw new InvalidOperationException($"Context '{contextId}' not found");

        // Thread-safe state update
        context.UpdateState(state);
        context.IncrementExecutionCount();

        _logger?.LogDebug(
            "Updated context {ContextId}, execution count: {Count}",
            contextId,
            context.ExecutionCount
        );
    }

    /// <inheritdoc/>
    public bool RemoveContext(string contextId)
    {
        if (string.IsNullOrWhiteSpace(contextId))
            return false;

        var removed = _contexts.TryRemove(contextId, out _);
        if (removed)
        {
            _logger?.LogDebug("Removed REPL context: {ContextId}", contextId);
        }

        return removed;
    }

    /// <inheritdoc/>
    public int CleanupExpiredContexts()
    {
        var expiredThreshold = DateTimeOffset.UtcNow - _contextTimeout;
        var expiredContexts = _contexts
            .Where(kvp => kvp.Value.LastAccessedAt < expiredThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        var removed = expiredContexts
            .Where(contextId => _contexts.TryRemove(contextId, out _))
            .Count();

        if (removed > 0)
        {
            _logger?.LogInformation("Cleaned up {Count} expired REPL contexts", removed);
        }

        return removed;
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetActiveContexts()
    {
        return _contexts.Keys.ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public ReplContextMetadata? GetContextMetadata(string contextId)
    {
        if (string.IsNullOrWhiteSpace(contextId))
            return null;

        if (_contexts.TryGetValue(contextId, out var context))
        {
            return new ReplContextMetadata
            {
                ContextId = context.ContextId,
                CreatedAt = context.CreatedAt,
                LastAccessedAt = context.LastAccessedAt,
                ExecutionCount = context.ExecutionCount,
                IsInitialized = context.IsInitialized,
            };
        }

        return null;
    }
}
