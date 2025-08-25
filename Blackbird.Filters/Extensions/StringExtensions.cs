using System.Text.RegularExpressions;

namespace Blackbird.Filters.Extensions;
public static class StringExtensions
{
    public static Stream ToStream(this string s)
    {
        MemoryStream memoryStream = new();
        StreamWriter streamWriter = new StreamWriter(memoryStream);
        streamWriter.Write(s);
        streamWriter.Flush();
        memoryStream.Position = 0L;
        return memoryStream;
    }
    
    public static string RemoveIdeFormatting(this string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;

        var result = Regex.Replace(s, @"\r\n?|\n|\t", " ");
        result = Regex.Replace(result, @" {2,}", " ");

        return result;
    }

    public static string? NullIfEmpty(this string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return s;
    }
}
