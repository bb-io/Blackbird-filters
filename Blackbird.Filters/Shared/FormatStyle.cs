using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using HtmlAgilityPack;

namespace Blackbird.Filters.Shared;
public class FormatStyle
{
    public HtmlTag? Tag { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = [];

    public string ToHtml(string innerHtml)
    {
        if (!Tag.HasValue) return innerHtml;
        var doc = new HtmlDocument();
        var node = doc.CreateElement(Tag.Value.ToTag());
        node.InnerHtml = innerHtml;
        return node.OuterHtml;
    }

    internal string GetPartialTag()
    {
        if (!Tag.HasValue) return string.Empty;
        return $"{Tag.Value.ToTag()} {Attributes.ToAttributeString()}".Trim();
    }
}
