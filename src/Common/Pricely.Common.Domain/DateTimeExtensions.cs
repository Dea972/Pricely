namespace Pricely.Common.Domain;

public static class DateTimeExtensions
{
    /// <summary>
    /// Returns the instant as a UTC <see cref="DateTime"/>: a local time is converted, an unspecified one is taken as
    /// already being UTC. The database columns are <c>timestamptz</c> and Npgsql refuses any other kind.
    /// </summary>
    public static DateTime AsUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
