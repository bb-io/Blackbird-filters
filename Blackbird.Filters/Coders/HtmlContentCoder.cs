using Blackbird.Filters.Content;
using Blackbird.Filters.Content.Tags;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using System.Net.Mime;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace Blackbird.Filters.Coders;
public static class HtmlContentCoder
{
    /// <summary>
    /// Turn any Html content into coded content.
    /// </summary>
    /// <param name="content">The HTML string</param>
    /// <returns></returns>
    public static CodedContent Deserialize(string content, string fileName)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var codedContent = new CodedContent(fileName, MediaTypeNames.Text.Html, content)
        {
            TextUnits = ExtractTextUnits(doc.DocumentNode),
        };

        codedContent.Language = GetHtmlLangAttribute(doc);
        codedContent.UniqueContentId = GetBlackbirdUcid(doc);

        return codedContent;
    }

    private static string? GetHtmlLangAttribute(HtmlDocument document)
    {
        var htmlNode = document.DocumentNode.SelectSingleNode("//html");

        if (htmlNode != null && htmlNode.Attributes["lang"] != null)
        {
            return htmlNode.Attributes["lang"].Value;
        }

        return null;
    }

    private static string? GetBlackbirdUcid(HtmlDocument document)
    {
        var metaNode = document.DocumentNode.SelectSingleNode("//meta[@name='blackbird-ucid']");

        if (metaNode != null && metaNode.Attributes["content"] != null)
        {
            return metaNode.Attributes["content"].Value;
        }

        return null;
    }

    /// <summary>
    /// Turn HTML coded content back into a plain HTML string
    /// </summary>
    /// <param name="content">An HTML coded content representation</param>
    /// <returns></returns>
    /// <exception cref="Exception">When the representation has malformed location references to the original HTML file.</exception>
    public static string Serialize(CodedContent content)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(content.Original);

        if (content.UniqueContentId != null)
        {
            SetOrUpdateBlackbirdUcid(doc, content.UniqueContentId);
        }

        foreach(var unit in content.TextUnits)
        {
            if (unit.Reference is null) continue;
            var node = doc.DocumentNode.SelectSingleNode(unit.Reference.Replace("/#text", "/text()"));
            if (node is null) throw new Exception($"Malformed unit location reference. Node not found in the raw HTML document. ({unit.Reference})");

            Match match = Regex.Match(unit.Reference, @"@([a-zA-Z_][\w\-]*)");
            if (match.Success)
            {
                node.Attributes[match.Groups[1].Value].Value = unit.GetCodedText();
            }
            else
            {
                node.InnerHtml = unit.GetCodedText();
            }
        }

        return doc.DocumentNode.OuterHtml;
    }

    private static void SetOrUpdateBlackbirdUcid(HtmlDocument document, string ucidValue)
    {
        var headNode = document.DocumentNode.SelectSingleNode("//head");
        if (headNode == null)
        {
            // Create <head> if it doesn't exist
            headNode = document.CreateElement("head");
            var htmlNode = document.DocumentNode.SelectSingleNode("//html");
            if (htmlNode != null)
                htmlNode.PrependChild(headNode);
            else
                document.DocumentNode.AppendChild(headNode);
        }

        var metaNode = headNode.SelectSingleNode("meta[@name='blackbird-ucid']");
        if (metaNode != null)
        {
            // Update existing
            metaNode.SetAttributeValue("content", ucidValue);
        }
        else
        {
            // Create new
            var newMeta = document.CreateElement("meta");
            newMeta.SetAttributeValue("name", "blackbird-ucid");
            newMeta.SetAttributeValue("content", ucidValue);
            headNode.AppendChild(newMeta);
        }
    }


    /// <summary>
    /// Checks if the provided string is actually an HTML file.
    /// </summary>
    /// <param name="content">The supposed HTMl content string</param>
    /// <returns>True if this is HTML. False otherwise.</returns>
    public static bool IsHtml(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var htmlTagPattern = @"<\s*(html|body|head|title|div|span|p|a|!DOCTYPE)";
        if (!Regex.IsMatch(content, htmlTagPattern, RegexOptions.IgnoreCase))
            return false;

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            var root = doc.DocumentNode;
            return root.Descendants().Any(n => n.Name.Equals("html", StringComparison.OrdinalIgnoreCase) ||
                                               n.Name.Equals("body", StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    internal static List<TextUnit> ExtractTextUnits(HtmlNode node)
    {
        var units = new List<TextUnit>();

        if (node.IsIgnoredElement()) return units;

        foreach (var attribute in node.GetTranslatableAttributes()) 
        {
            units.Add(BuildUnit(attribute));
        }

        if (node.NodeType == HtmlNodeType.Text)
        {
            if (!string.IsNullOrWhiteSpace(node.InnerText))
            {
                units.AddRange(BuildUnits(node));
            }
            return units;
        }

        if (!node.HasChildNodes) return units;

        if (!string.IsNullOrWhiteSpace(node.InnerText) && node.ChildNodes.All(x => x.IsInlineElement() || x.NodeType != HtmlNodeType.Element))
        {
            units.AddRange(BuildUnits(node));
            return units;
        }

        foreach (var child in node.ChildNodes)
        {
            units.AddRange(ExtractTextUnits(child));
        }

        return units;
    }

    internal static List<TextUnit> BuildUnits(HtmlNode node)
    {
        var unit = new TextUnit(node.XPath, MediaTypeNames.Text.Html);

        if (node.NodeType == HtmlNodeType.Text)
        {
            unit.Parts.Add(new TextPart { Value = node.InnerText.RemoveIdeFormatting() });
        }
        else
        {
            unit.Parts = BuildTextParts(node.ChildNodes);
        }

        if (unit.Parts.Count > 0) 
        {
            unit.Parts[0].Value = unit.Parts[0].Value.TrimStart();
            unit.Parts[unit.Parts.Count - 1].Value = unit.Parts[unit.Parts.Count - 1].Value.TrimEnd();
        }

        var units = new List<TextUnit>();
        foreach (InlineCode textPart in unit.Parts.Where(x => x is InlineCode))
        {
            units.AddRange(textPart.UnitReferences);
        }
        units.Add(unit);
        return units;
    }

    internal static List<TextPart> BuildTextParts(HtmlNodeCollection nodes)
    {
        var parts = new List<TextPart>();
        foreach (var child in nodes)
        {
            if (child.NodeType == HtmlNodeType.Text)
            {
                parts.Add(new TextPart { Value = child.InnerText.RemoveIdeFormatting() });
            }
            else if (child.NodeType == HtmlNodeType.Element)
            {
                var subUnits = new List<TextUnit>();
                foreach (var attribute in child.GetTranslatableAttributes())
                {
                    subUnits.Add(BuildUnit(attribute));
                }

                if (child.ChildNodes.Count() == 0)
                {
                    parts.Add(new InlineCode { Value = child.OuterHtml, UnitReferences = subUnits });
                }
                else
                {
                    var (start, _, end) = ParseHtmlParts(child.OuterHtml);
                    var startTag = new StartCode { Value = start, UnitReferences = subUnits };
                    var endTag = new EndCode { Value = end, StartCode = startTag, UnitReferences = subUnits };
                    startTag.EndCode = endTag;
                    parts.Add(startTag);
                    parts.AddRange(BuildTextParts(child.ChildNodes));
                    parts.Add(endTag);
                }
            }
        }
        return parts;
    }

    private static TextUnit BuildUnit(HtmlAttribute attribute) => new(attribute.XPath, MediaTypeNames.Text.Plain) { Parts = [new TextPart { Value = attribute.Value }] };

    private static (string StartTag, string Content, string EndTag) ParseHtmlParts(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return (string.Empty, string.Empty, string.Empty);

        var match = Regex.Match(html, @"^(<[^>]+>)(.*?)(</[^>]+>)$", RegexOptions.Singleline);

        if (match.Success)
        {
            string startTag = match.Groups[1].Value;
            string content = match.Groups[2].Value;
            string endTag = match.Groups[3].Value;

            return (startTag, content, endTag);
        }

        return (string.Empty, string.Empty, string.Empty);
    }

    // https://www.w3schools.com/htmL/html_blocks.asp
    // https://html.spec.whatwg.org/multipage/dom.html#phrasing-content-2
    private static List<string> InlineElements = ["a", "abbr", "area", "audio", "acronym", "b", "bdi", "bdo", "big", "br", "button", "canvas", "cite", "code", "data", "datalist", "del", "dfn", "em", "embed", "i", "iframe", "img", "input", "ins", "kbd", "label", "map", "object", "output", "picture", "progress", "q", "s", "samp", "script", "select", "slot", "small", "span", "strong", "sub", "sup", "svg", "textarea", "time", "u", "tt", "var"];
    private static List<string> IgnoredElements = ["script", "style"];
    private static List<string> TranslatableAttributes = ["alt", "title", "content", "placeholder"];

    private static bool IsInlineElement(this HtmlNode node)
    {
        return InlineElements.Contains(node.Name);
    }

    private static bool IsIgnoredElement(this HtmlNode node)
    {
        return IgnoredElements.Contains(node.Name);
    }

    private static List<HtmlAttribute> GetTranslatableAttributes(this HtmlNode node)
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