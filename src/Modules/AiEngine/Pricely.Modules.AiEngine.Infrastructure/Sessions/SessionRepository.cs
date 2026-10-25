using Microsoft.EntityFrameworkCore;
using Pricely.Modules.AiEngine.Domain.Sessions;
using Pricely.Modules.AiEngine.Infrastructure.Database;

namespace Pricely.Modules.AiEngine.Infrastructure.Sessions;

internal sealed class SessionRepository(AiEngineDbContext context) : ISessionRepository
{
    public Task<Session?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
        context.Sessions.FirstOrDefaultAsync(s => s.ExternalId == externalId, cancellationToken);

    public void Insert(Session session) => context.Sessions.Add(session);
}
