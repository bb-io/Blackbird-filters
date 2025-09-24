using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using HtmlAgilityPack;

namespace Blackbird.Filters.Shared;
public class FormatStyle
{
    public HtmlTag? Tag { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = [];

    public string ToHtml(string innerHtml, string reference)
    {
        if (!Tag.HasValue) return innerHtml;
        var doc = new HtmlDocument();
        var node = doc.CreateElement(Tag.Value.ToTag());
        foreach(var attribute in Attributes)
        {
            node.Attributes.Add(attribute.Key, attribute.Value);
        }

        foreach (var attribute in HtmlNodeExtensions.TranslatableAttributes)
        {
            if (reference.Contains(attribute, StringComparison.InvariantCultureIgnoreCase))
            {
                node.Attributes[attribute].Value = innerHtml;
                return node.OuterHtml;
            }
        }

        node.InnerHtml = innerHtml;
        return node.OuterHtml;
    }

    internal string GetPartialTag()
    {
        if (!Tag.HasValue) return string.Empty;
        return $"{Tag.Value.ToTag()} {Attributes.ToAttributeString()}".Trim();
    }
}
