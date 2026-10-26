namespace Pricely.Modules.Agentic.Application.Abstractions.Data;

public interface IUnitOfWork
{
    /// <summary>
    /// Saves the changes. Throws <see cref="ConcurrentUpdateException"/> when the same conversation or action was
    /// changed by another request in the meantime (optimistic concurrency).
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
