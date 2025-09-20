using System.Net;

namespace Blackbird.Filters.Extensions;
public static class DictionaryExtensions
{
    public static string ToAttributeString(this IDictionary<string, string> attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return string.Empty;

        return string.Join(" ",
            attributes
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                .Select(kv =>
                {
                    var value = kv.Value ?? string.Empty;
                    var encoded = WebUtility.HtmlEncode(value);
                    return $"{kv.Key}=\"{encoded}\"";
                }));
    }
}
