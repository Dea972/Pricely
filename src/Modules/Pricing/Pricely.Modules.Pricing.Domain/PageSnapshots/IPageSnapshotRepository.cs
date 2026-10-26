namespace Pricely.Modules.Pricing.Domain.PageSnapshots;

public interface IPageSnapshotRepository
{
    Task<PageSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(PageSnapshot snapshot);
}
