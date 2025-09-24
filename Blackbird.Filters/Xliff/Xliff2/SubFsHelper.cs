using System.Text;
using System.Text.RegularExpressions;

namespace Blackbird.Filters.Xliff.Xliff2;
public static class SubFsHelper
{
    public static string? ToSubFsString(Dictionary<string, string> attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return null;

        return string.Join('\\', attributes.Select(x => $"{Escape(x.Key)},{Escape(x.Value)}"));
    }

    public static Dictionary<string, string> ParseSubFsString(string? subFs)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(subFs))
            return result;

        var parts = SplitOnSingleBackslash(subFs);

        foreach (var part in parts)
        {
            var kv = SplitOnUnescapedComma(part);
            if (kv.Length != 2)
                continue;

            string key = Unescape(kv[0]);
            string value = Unescape(kv[1]);

            if (!string.IsNullOrEmpty(key))
                result[key] = value;
        }

        return result;
    }

    private static string[] SplitOnSingleBackslash(string input)
    {
        string pattern = @"(?<!\\)\\(?![\\,])";
        return Regex.Split(input, pattern);
    }

    private static string[] SplitOnUnescapedComma(string input)
    {
        string pattern = @"(?<!\\),";
        return Regex.Split(input, pattern);
    }

    private static string Escape(string input)
    {
        if (input == null) return "";
        return input
            .Replace("\\", "\\\\")
            .Replace(",", "\\,");
    }

    private static string Unescape(string input)
    {
        if (input == null) return "";
        return input
            .Replace("\\\\", "\\")
            .Replace("\\,", ",");
    }    
}
