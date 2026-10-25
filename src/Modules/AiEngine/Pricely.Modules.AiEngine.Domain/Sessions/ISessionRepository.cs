namespace Pricely.Modules.AiEngine.Domain.Sessions;

public interface ISessionRepository
{
    Task<Session?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    void Insert(Session session);
}
