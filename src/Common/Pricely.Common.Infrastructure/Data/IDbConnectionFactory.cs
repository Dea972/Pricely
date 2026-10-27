using System.Data.Common;

namespace Pricely.Common.Infrastructure.Data;

public interface IDbConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync();
}
