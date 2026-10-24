using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricely.Common.Application.Extensions;
public static class StringExtensions
{
    /// <summary>
    /// Determines whether the specified string is null, empty, or consists only of white-space characters.
    /// </summary>
    /// <remarks>This method extends the standard null or empty check by also returning <see langword="true"/>
    /// if the string contains only white-space characters, such as spaces or tabs.</remarks>
    /// <param name="value">The string to test for null, emptiness, or white-space content.</param>
    /// <returns><see langword="true"/> if the <paramref name="value"/> parameter is null, an empty string (""), or consists only
    /// of white-space characters; otherwise, <see langword="false"/>.</returns>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrWhiteSpace(value?.Trim());
    }

    /// <summary>
    /// Returns a substring of the specified length from the beginning of the string, or an empty string if the input is
    /// null, empty, or the length is not positive.
    /// </summary>
    /// <param name="value">The string from which to extract the substring. Can be null.</param>
    /// <param name="length">The maximum number of characters to include in the substring. Must be greater than 0 to return any characters.</param>
    /// <returns>A substring containing at most the specified number of characters from the start of the input string, or an
    /// empty string if the input is null, empty, or the length is less than or equal to zero.</returns>
    public static string SafeSubstring(this string? value, int length)
    {
        if (string.IsNullOrEmpty(value) || length <= 0)
        {
            return string.Empty;
        }
        return value.Length <= length ? value : value.Substring(0, length);
    }
}
