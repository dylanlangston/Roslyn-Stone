using Microsoft.CodeAnalysis.CSharp.Scripting;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for ExecutionContextManager
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "ContextManagement")]
public class ReplContextManagerTests
{
    [Fact]
    [Trait("Feature", "ContextCreation")]
    public void CreateContext_ReturnsUniqueId()
    {
        // Arrange
        var manager = new ExecutionContextManager();

        // Act
        var contextId = manager.CreateContext();

        // Assert
        Assert.NotNull(contextId);
        Assert.NotEmpty(contextId);
        Assert.True(Guid.TryParse(contextId, out _));
    }

    [Fact]
    [Trait("Feature", "ContextCreation")]
    public void CreateContext_MultipleCalls_ReturnsDifferentIds()
    {
        // Arrange
        var manager = new ExecutionContextManager();

        // Act
        var contextId1 = manager.CreateContext();
        var contextId2 = manager.CreateContext();

        // Assert
        Assert.NotEqual(contextId1, contextId2);
    }

    [Fact]
    [Trait("Feature", "ContextExists")]
    public void ContextExists_ExistingContext_ReturnsTrue()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var contextId = manager.CreateContext();

        // Act
        var exists = manager.ContextExists(contextId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    [Trait("Feature", "ContextExists")]
    public void ContextExists_NonExistingContext_ReturnsFalse()
    {
        // Arrange
        var manager = new ExecutionContextManager();

        // Act
        var exists = manager.ContextExists("non-existing-id");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    [Trait("Feature", "ContextExists")]
    public void ContextExists_NullOrEmpty_ReturnsFalse()
    {
        // Arrange
        var manager = new ExecutionContextManager();

        // Act & Assert
        Assert.False(manager.ContextExists(null!));
        Assert.False(manager.ContextExists(string.Empty));
        Assert.False(manager.ContextExists("   "));
    }

    [Fact]
    [Trait("Feature", "ContextState")]
    public void GetContextState_NewContext_ReturnsNull()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var contextId = manager.CreateContext();

        // Act
        var state = manager.GetContextState(contextId);

        // Assert
        Assert.Null(state);
    }

    [Fact]
    [Trait("Feature", "ContextState")]
    public async Task UpdateContextState_ValidContext_UpdatesState()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var contextId = manager.CreateContext();
        var scriptState = await CSharpScript.RunAsync("var x = 10;");

        // Act
        manager.UpdateContextState(contextId, scriptState);
        var retrievedState = manager.GetContextState(contextId);

        // Assert
        Assert.NotNull(retrievedState);
        Assert.Equal(scriptState, retrievedState);
    }

    [Fact]
    [Trait("Feature", "ContextState")]
    public async Task UpdateContextState_InvalidContext_ThrowsException()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var scriptState = await CSharpScript.RunAsync("var x = 10;");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            manager.UpdateContextState("non-existing-id", scriptState)
        );
    }

    [Fact]
    [Trait("Feature", "ContextState")]
    public async Task UpdateContextState_NullContextId_ThrowsException()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var scriptState = await CSharpScript.RunAsync("var x = 10;");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => manager.UpdateContextState(null!, scriptState));
    }

    [Fact]
    [Trait("Feature", "ContextRemoval")]
    public void RemoveContext_ExistingContext_ReturnsTrue()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var contextId = manager.CreateContext();

        // Act
        var removed = manager.RemoveContext(contextId);

        // Assert
        Assert.True(removed);
        Assert.False(manager.ContextExists(contextId));
    }

    [Fact]
    [Trait("Feature", "ContextRemoval")]
    public void RemoveContext_NonExistingContext_ReturnsFalse()
    {
        // Arrange
        var manager = new ExecutionContextManager();

        // Act
        var removed = manager.RemoveContext("non-existing-id");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    [Trait("Feature", "ContextCleanup")]
    public async Task CleanupExpiredContexts_ExpiredContexts_RemovesThem()
    {
        // Arrange
        var shortTimeout = TimeSpan.FromMilliseconds(100);
        var manager = new ExecutionContextManager(contextTimeout: shortTimeout);
        var contextId1 = manager.CreateContext();
        var contextId2 = manager.CreateContext();

        // Wait for contexts to expire
        await Task.Delay(150);

        // Act
        var removed = manager.CleanupExpiredContexts();

        // Assert
        Assert.Equal(2, removed);
        Assert.False(manager.ContextExists(contextId1));
        Assert.False(manager.ContextExists(contextId2));
    }

    [Fact]
    [Trait("Feature", "ContextCleanup")]
    public void CleanupExpiredContexts_ActiveContexts_DoesNotRemove()
    {
        // Arrange
        var manager = new ExecutionContextManager(contextTimeout: TimeSpan.FromHours(1));
        var contextId = manager.CreateContext();

        // Act
        var removed = manager.CleanupExpiredContexts();

        // Assert
        Assert.Equal(0, removed);
        Assert.True(manager.ContextExists(contextId));
    }

    [Fact]
    [Trait("Feature", "ContextList")]
    public void GetActiveContexts_MultipleContexts_ReturnsAll()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var contextId1 = manager.CreateContext();
        var contextId2 = manager.CreateContext();
        var contextId3 = manager.CreateContext();

        // Act
        var activeContexts = manager.GetActiveContexts();

        // Assert
        Assert.Equal(3, activeContexts.Count);
        Assert.Contains(contextId1, activeContexts);
        Assert.Contains(contextId2, activeContexts);
        Assert.Contains(contextId3, activeContexts);
    }

    [Fact]
    [Trait("Feature", "ContextList")]
    public void GetActiveContexts_NoContexts_ReturnsEmpty()
    {
        // Arrange
        var manager = new ExecutionContextManager();

        // Act
        var activeContexts = manager.GetActiveContexts();

        // Assert
        Assert.Empty(activeContexts);
    }

    [Fact]
    [Trait("Feature", "ContextMetadata")]
    public void GetContextMetadata_ExistingContext_ReturnsMetadata()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var contextId = manager.CreateContext();

        // Act
        var metadata = manager.GetContextMetadata(contextId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(contextId, metadata.ContextId);
        Assert.Equal(0, metadata.ExecutionCount);
        Assert.False(metadata.IsInitialized);
    }

    [Fact]
    [Trait("Feature", "ContextMetadata")]
    public async Task GetContextMetadata_AfterExecution_ShowsUpdatedCount()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var contextId = manager.CreateContext();
        var scriptState = await CSharpScript.RunAsync("var x = 10;");

        // Act
        manager.UpdateContextState(contextId, scriptState);
        var metadata = manager.GetContextMetadata(contextId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(1, metadata.ExecutionCount);
        Assert.True(metadata.IsInitialized);
    }

    [Fact]
    [Trait("Feature", "ContextMetadata")]
    public void GetContextMetadata_NonExistingContext_ReturnsNull()
    {
        // Arrange
        var manager = new ExecutionContextManager();

        // Act
        var metadata = manager.GetContextMetadata("non-existing-id");

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    [Trait("Feature", "ThreadSafety")]
    public void CreateContext_ConcurrentCalls_AllSucceed()
    {
        // Arrange
        var manager = new ExecutionContextManager();
        var contextIds = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act
        Parallel.For(
            0,
            100,
            _ =>
            {
                var contextId = manager.CreateContext();
                contextIds.Add(contextId);
            }
        );

        // Assert
        Assert.Equal(100, contextIds.Count);
        Assert.Equal(100, contextIds.Distinct().Count()); // All unique
    }
}
