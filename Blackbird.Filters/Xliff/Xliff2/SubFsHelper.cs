using System.Text;

namespace Blackbird.Filters.Xliff.Xliff2;
public static class SubFsHelper
{
    public static string? ToSubFsString(Dictionary<string, string> attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return null;

        var sb = new StringBuilder();
        bool first = true;

        foreach (var kvp in attributes)
        {
            if (!first)
                sb.Append('\\');

            string key = Escape(kvp.Key);
            string value = Escape(kvp.Value);

            sb.Append(key).Append(',').Append(value);

            first = false;
        }

        return sb.ToString();
    }

    public static Dictionary<string, string> ParseSubFsString(string? subFs)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(subFs))
            return result;

        var parts = SplitByUnescaped(subFs, '\\');

        foreach (var part in parts)
        {
            var kv = SplitByUnescaped(part, ',');
            if (kv.Count != 2)
                continue;

            string key = Unescape(kv[0]);
            string value = Unescape(kv[1]);

            if (!string.IsNullOrEmpty(key))
                result[key] = value;
        }

        return result;
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
        var sb = new StringBuilder();
        bool escaping = false;

        foreach (char c in input)
        {
            if (escaping)
            {
                sb.Append(c);
                escaping = false;
            }
            else if (c == '\\')
            {
                escaping = true;
            }
            else
            {
                sb.Append(c);
            }
        }

        if (escaping)
            sb.Append('\\');

        return sb.ToString();
    }

    private static List<string> SplitByUnescaped(string input, char separator)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool escaping = false;

        foreach (char c in input)
        {
            if (escaping)
            {
                sb.Append('\\').Append(c);
                escaping = false;
            }
            else if (c == '\\')
            {
                escaping = true;
            }
            else if (c == separator)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        if (escaping)
            sb.Append('\\');

        result.Add(sb.ToString());
        return result;
    }
}
