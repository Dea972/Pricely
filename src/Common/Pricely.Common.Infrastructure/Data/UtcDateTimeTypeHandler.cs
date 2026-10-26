using System.Data;
using Dapper;
using Pricely.Common.Domain;

namespace Pricely.Common.Infrastructure.Data;

/// <summary>
/// Dapper sends every <see cref="DateTime"/> parameter as UTC. Query strings such as <c>?fromUtc=2026-01-01</c> bind to an
/// unspecified or local <see cref="DateTime"/>, which Npgsql refuses to write to a <c>timestamptz</c> parameter.
/// Values read from the database are left as they are.
/// </summary>
internal sealed class UtcDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value) => parameter.Value = value.AsUtc();

    public override DateTime Parse(object value) => (DateTime)value;
}
