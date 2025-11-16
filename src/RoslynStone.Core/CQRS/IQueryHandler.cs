namespace RoslynStone.Core.CQRS;

/// <summary>
/// Handler interface for queries
/// </summary>
/// <typeparam name="TQuery">The query type</typeparam>
/// <typeparam name="TResult">The result type</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handles the query asynchronously and returns a result
    /// </summary>
    /// <param name="query">The query to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation with the result</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
