using Blackbird.Filters.Constants;
using Blackbird.Filters.Content;
using Blackbird.Filters.Content.Tags;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Interfaces;
using Blackbird.Filters.Shared;
using HtmlAgilityPack;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace Blackbird.Filters.Coders;
public class HtmlContentCoder : IContentCoder
{
    public const string XPathSplitter = "~~";
    public List<HtmlTag?> InlineSplitOn = [HtmlTag.LineBreak];

    public IEnumerable<string> SupportedMediaTypes => [MediaTypeNames.Text.Html];

    /// <summary>
    /// Turn any Html content into coded content.
    /// </summary>
    /// <param name="content">The HTML string</param>
    /// <returns></returns>
    public CodedContent Deserialize(string content, string fileName)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var codedContent = new CodedContent(fileName, MediaTypeNames.Text.Html, content)
        {
            TextUnits = ExtractTextUnits(doc.DocumentNode),
        };

        codedContent.Language = GetHtmlLangAttribute(doc);

        codedContent.SystemReference.ContentId = GetBlackbirdMeta(doc, Meta.Types.ContentId);
        codedContent.SystemReference.ContentName = GetBlackbirdMeta(doc, Meta.Types.ContentName);
        codedContent.SystemReference.AdminUrl = GetBlackbirdMeta(doc, Meta.Types.AdminUrl);
        codedContent.SystemReference.PublicUrl = GetBlackbirdMeta(doc, Meta.Types.PublicUrl);
        codedContent.SystemReference.SystemName = GetBlackbirdMeta(doc, Meta.Types.SystemName);
        codedContent.SystemReference.SystemRef = GetBlackbirdMeta(doc, Meta.Types.SystemRef);

        codedContent.Provenance.Translation.Person = GetBodyAttribute(doc, "its-person");
        codedContent.Provenance.Translation.PersonReference = GetBodyAttribute(doc, "its-person-ref");
        codedContent.Provenance.Translation.Organization = GetBodyAttribute(doc, "its-org");
        codedContent.Provenance.Translation.OrganizationReference = GetBodyAttribute(doc, "its-org-ref");
        codedContent.Provenance.Translation.Tool = GetBodyAttribute(doc, "its-tool");
        codedContent.Provenance.Translation.ToolReference = GetBodyAttribute(doc, "its-tool-ref");

        codedContent.Provenance.Review.Person = GetBodyAttribute(doc, "its-rev-person");
        codedContent.Provenance.Review.PersonReference = GetBodyAttribute(doc, "its-rev-person-ref");
        codedContent.Provenance.Review.Organization = GetBodyAttribute(doc, "its-rev-org");
        codedContent.Provenance.Review.OrganizationReference = GetBodyAttribute(doc, "its-rev-org-ref");
        codedContent.Provenance.Review.Tool = GetBodyAttribute(doc, "its-rev-tool");
        codedContent.Provenance.Review.ToolReference = GetBodyAttribute(doc, "its-rev-tool-ref");

        return codedContent;
    }

    private string? GetHtmlLangAttribute(HtmlDocument document)
    {
        var htmlNode = document.DocumentNode.SelectSingleNode("//html");

        if (htmlNode != null && htmlNode.Attributes["lang"] != null)
        {
            return htmlNode.Attributes["lang"].Value;
        }

        return null;
    }

    private string? GetBodyAttribute(HtmlDocument document, string name)
    {
        var bodyNode = document.DocumentNode.SelectSingleNode("//body");

        if (bodyNode != null && bodyNode.Attributes[name] != null)
        {
            return bodyNode.Attributes[name].Value;
        }

        return null;
    }

    private string? GetBlackbirdMeta(HtmlDocument document, string name)
    {
        var metaNode = document.DocumentNode.SelectSingleNode($"//meta[@name='blackbird-{name}']");

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
    /// <exception cref="ArgumentException">When the representation has malformed location references to the original HTML file.</exception>
    public string Serialize(CodedContent content)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(content.Original);

        SetOrUpdateBlackbirdMeta(doc, Meta.Types.ContentId, content.SystemReference.ContentId);
        SetOrUpdateBlackbirdMeta(doc, Meta.Types.ContentName, content.SystemReference.ContentName);
        SetOrUpdateBlackbirdMeta(doc, Meta.Types.AdminUrl, content.SystemReference.AdminUrl);
        SetOrUpdateBlackbirdMeta(doc, Meta.Types.PublicUrl, content.SystemReference.PublicUrl);
        SetOrUpdateBlackbirdMeta(doc, Meta.Types.SystemName, content.SystemReference.SystemName);
        SetOrUpdateBlackbirdMeta(doc, Meta.Types.SystemRef, content.SystemReference.SystemRef);


        var splitUnits = content.TextUnits.GroupBy(x => x.Reference.Split(XPathSplitter)[0]);

        foreach(var unitGroup in splitUnits)
        {
            var xpath = unitGroup.Key;
            if (xpath is null) continue;

            var node = doc.DocumentNode.SelectSingleNode(xpath.Replace("/#text", "/text()"));
            if (node is null) throw new InvalidOperationException($"Malformed unit location reference. Node not found in the raw HTML document. ({xpath})");

            if (unitGroup.Count() == 1)
            {
                var unit = unitGroup.First();
                Match match = Regex.Match(xpath, @"@([a-zA-Z_][\w\-]*)");
                if (match.Success)
                {
                    node.Attributes[match.Groups[1].Value].Value = unit.GetCodedText();
                }
                else
                {
                    node.InnerHtml = unit.GetCodedText();
                }
            }
            else
            {
                var codedText = string.Join(string.Empty, unitGroup.Select(x => x.GetCodedText()));
                node.InnerHtml = codedText;
            }
        }


        return doc.DocumentNode.OuterHtml;
    }

    private void SetOrUpdateBlackbirdMeta(HtmlDocument document, string name, string? value)
    {
        if (value is null) return;

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

        var metaNode = headNode.SelectSingleNode($"meta[@name='blackbird-{name}']");
        if (metaNode != null)
        {
            // Update existing
            metaNode.SetAttributeValue("content", value);
        }
        else
        {
            // Create new
            var newMeta = document.CreateElement("meta");
            newMeta.SetAttributeValue("name", $"blackbird-{name}");
            newMeta.SetAttributeValue("content", value);
            headNode.AppendChild(newMeta);
        }
    }


    /// <summary>
    /// Checks if the provided string is actually an HTML file.
    /// </summary>
    /// <param name="content">The supposed HTMl content string</param>
    /// <returns>True if this is HTML. False otherwise.</returns>
    public bool CanProcessContent(string content)
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

    internal List<TextUnit> ExtractTextUnits(HtmlNode node, string? key = null)
    {
        var units = new List<TextUnit>();

        if (node.IsIgnoredElement()) return units;

        key ??= node.GetKey();

        foreach (var attribute in node.GetTranslatableAttributes()) 
        {
            units.Add(BuildUnit(attribute, node, key));
        }

        if (node.NodeType == HtmlNodeType.Text)
        {
            if (!string.IsNullOrWhiteSpace(node.InnerText))
            {
                units.AddRange(BuildUnits(node, key));
            }
            return units;
        }

        if (!node.HasChildNodes) return units;

        if (!string.IsNullOrWhiteSpace(node.InnerText) && node.ChildNodes.All(x => x.IsInlineElement() || x.NodeType != HtmlNodeType.Element))
        {
            units.AddRange(BuildUnits(node, key));
            return units;
        }

        foreach (var child in node.ChildNodes)
        {
            units.AddRange(ExtractTextUnits(child, key));
        }

        return units;
    }

    internal List<TextUnit> BuildUnits(HtmlNode node, string? key = null)
    {      
        key ??= node.GetKey();
        var unit = new TextUnit(node.XPath, this) 
        { 
            Key = key, 
            SizeRestrictions = SizeRestrictionHelper.Deserialize(node.GetAttributeValue(Meta.Html.SizeRestriction, string.Empty)) 
        };

        unit.FormatStyle = GetFormatStyle(node);

        if (node.NodeType == HtmlNodeType.Text)
        {
            unit.Parts.Add(new TextPart { Value = node.InnerText.RemoveIdeFormatting() });
        }
        else
        {
            unit.Parts = BuildTextParts(node.ChildNodes, key);
        }

        if (unit.Parts.Count > 0) 
        {
            unit.Parts[0].Value = unit.Parts[0].Value.TrimStart();
            unit.Parts[unit.Parts.Count - 1].Value = unit.Parts[unit.Parts.Count - 1].Value.TrimEnd();
        }

        var units = SplitUnitOnInlineParts(unit);
        foreach (InlineCode textPart in units.SelectMany(x => x.Parts.Where(x => x is InlineCode)).ToList())
        {
            units.AddRange(textPart.UnitReferences);
        }

        return units;
    }

    internal List<TextUnit> SplitUnitOnInlineParts(TextUnit unit)
    {
        var units = new List<TextUnit>();

        var IsSplitOnTagPart = (TextPart? part) => {
            return part is InlineCode inlineCode && InlineSplitOn.Contains(inlineCode.FormatStyle.Tag);
        };

        if (!unit.Parts.OfType<InlineCode>().Any(x => IsSplitOnTagPart(x)))
        {
            return [unit];
        }

        var GetTextPart = (int index) => {
            return index < unit.Parts.Count ? unit.Parts[index] : null;
        };

        var previousSliceEnd = 0;
        for (int i = 0; i < unit.Parts.Count; i++)
        {
            var textPart = GetTextPart(i);
            if (IsSplitOnTagPart(textPart) && !IsSplitOnTagPart(GetTextPart(i + 1)) && textPart is InlineCode inlineCode)
            {
                var tagCode = inlineCode.FormatStyle.Tag?.ToTag();
                var newUnit = new TextUnit(unit.Reference + XPathSplitter + units.Count, this)
                {
                    Key = unit.Key,
                    SizeRestrictions = unit.SizeRestrictions,
                    FormatStyle = unit.FormatStyle,
                };

                newUnit.Parts = unit.Parts.Skip(previousSliceEnd).Take(i - previousSliceEnd + 1).ToList();
                previousSliceEnd = i + 1;

                units.Add(newUnit);
            }
        }

        if (previousSliceEnd < unit.Parts.Count)
        {
            var lastUnit = new TextUnit(unit.Reference + XPathSplitter + units.Count, this)
            {
                Key = unit.Key,
                SizeRestrictions = unit.SizeRestrictions,
            };
            lastUnit.Parts = unit.Parts.Skip(previousSliceEnd).Take(unit.Parts.Count - previousSliceEnd).ToList();
            units.Add(lastUnit);
        }

        return units;
    }

    internal List<TextPart> BuildTextParts(HtmlNodeCollection nodes, string? key = null)
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
                    subUnits.Add(BuildUnit(attribute, child, key));
                }

                if (child.ChildNodes.Count() == 0)
                {
                    var code = new InlineCode { Value = child.OuterHtml, UnitReferences = subUnits };
                    code.FormatStyle = GetFormatStyle(child);
                    parts.Add(code);
                }
                else
                {
                    var (start, _, end) = ParseHtmlParts(child.OuterHtml);
                    var startTag = new StartCode { Value = start, UnitReferences = subUnits };
                    var endTag = new EndCode { Value = end, StartCode = startTag, UnitReferences = subUnits };
                    startTag.FormatStyle = GetFormatStyle(child);
                    endTag.FormatStyle.Tag = startTag.FormatStyle.Tag;
                    startTag.EndCode = endTag;
                    parts.Add(startTag);
                    parts.AddRange(BuildTextParts(child.ChildNodes, key));
                    parts.Add(endTag);
                }
            }
        }
        return parts;
    }

    private TextUnit BuildUnit(HtmlAttribute attribute, HtmlNode originalNode, string? key = null)
    {
        return new(attribute.XPath, new PlaintextContentCoder()) { Key = key, Parts = [new TextPart { Value = attribute.Value }], FormatStyle = GetFormatStyle(originalNode) };
    }

    private FormatStyle GetFormatStyle(HtmlNode node)
    {
        var result = new FormatStyle();
        if (HtmlTagExtensions.TryParseTag(node.Name, out var parsed))
        {
            result.Tag = parsed;
            result.Attributes = node.Attributes.Where(x => !x.Name.StartsWith("data-")).ToDictionary(x => x.Name, x => x.Value);
        }

        return result;
    }
    
    private (string StartTag, string Content, string EndTag) ParseHtmlParts(string html)
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

    public List<TextPart> DeserializeSegment(string segment)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(segment);
            return BuildTextParts(doc.DocumentNode.ChildNodes);
        }
        catch (Exception)
        {
            return [new() { Value = segment }];
        }
    }

    public string NormalizeSegment(string segment)
    {
        var doc = new HtmlDocument { OptionWriteEmptyNodes = true };
        doc.LoadHtml(segment);
        foreach (var node in doc.DocumentNode.Descendants().Where(n => n.NodeType == HtmlNodeType.Element))
        {
            node.Attributes.RemoveAll();
        }

        return doc.DocumentNode.InnerHtml;
    }
}