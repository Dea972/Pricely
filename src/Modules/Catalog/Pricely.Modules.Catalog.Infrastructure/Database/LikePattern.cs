namespace Pricely.Modules.Catalog.Infrastructure.Database;

/// <summary>Text compared with <c>ILIKE</c> (PostgreSQL is case sensitive, ADR-012): its wildcards taken literally.</summary>
internal static class LikePattern
{
    public static string Escape(string text) =>
        text.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
