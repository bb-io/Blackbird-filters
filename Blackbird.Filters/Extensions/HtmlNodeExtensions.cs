using HtmlAgilityPack;

namespace Blackbird.Filters.Extensions;
public static class HtmlNodeExtensions
{
    // https://www.w3schools.com/htmL/html_blocks.asp
    // https://html.spec.whatwg.org/multipage/dom.html#phrasing-content-2
    public static List<string> InlineElements = ["a", "abbr", "area", "audio", "acronym", "b", "bdi", "bdo", "big", "br", "button", "canvas", "cite", "code", "data", "datalist", "del", "dfn", "em", "embed", "i", "iframe", "img", "input", "ins", "kbd", "label", "map", "object", "output", "picture", "progress", "q", "s", "samp", "script", "select", "slot", "small", "span", "strong", "sub", "sup", "svg", "textarea", "time", "u", "tt", "var"];
    public static List<string> IgnoredElements = ["script", "style"];
    public static List<string> TranslatableAttributes = ["alt", "title", "content", "placeholder"];

    public static bool IsInlineElement(this HtmlNode node)
    {
        return InlineElements.Contains(node.Name);
    }

    public static bool IsIgnoredElement(this HtmlNode node)
    {
        return IgnoredElements.Contains(node.Name);
    }

    public static string? GetKey(this HtmlNode node)
    {
        if (node.Attributes.Contains("data-blackbird-key"))
        {
            return node.Attributes["data-blackbird-key"].Value;
        }

        return null;
    }

    public static List<HtmlAttribute> GetTranslatableAttributes(this HtmlNode node)
    {
        var attributes = new List<HtmlAttribute>();
        foreach (var attribute in TranslatableAttributes)
        {
            if (node.Attributes.Contains(attribute))
            {
                if (node.Attributes.Contains("name") && node.Attributes["name"].Value.Contains("blackbird")) continue;
                attributes.Add(node.Attributes[attribute]);
            }
        }
        return attributes;
    }
}
