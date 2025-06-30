using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Content.Tags;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations.Tags;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;

public class Segment
{
    public string? Id { get; internal set; }
    public List<TextPart> Source { get; set; } = [];
    public List<TextPart> Target { get; set; } = [];
    public bool? CanResegment { get; set; }
    public SegmentState? State { get; set; }
    public string? SubState { get; set; }
    public bool? Ignorable { get; set; }
    public int? Order { get; set; }
    public WhiteSpaceHandling SourceWhiteSpaceHandling { get; set; } = WhiteSpaceHandling.Default;
    public WhiteSpaceHandling TargetWhiteSpaceHandling { get; set; } = WhiteSpaceHandling.Default;
    public List<XAttribute> SourceAttributes { get; set; } = [];
    public List<XAttribute> TargetAttributes { get; set; } = [];
    public CodeType? CodeType { get; set; }
    public bool IsIgnorbale => Ignorable.HasValue && Ignorable.Value;
    public bool IsInitial => State.HasValue ? State.Value == SegmentState.Initial : true;

    /// <summary>
    /// Get the source text including tags
    /// </summary>
    /// <returns>The source including tags</returns>
    public string GetSource()
    {
        if (CodeType.HasValue && CodeType.Value == Enums.CodeType.Html)
        {
            var codedText = string.Join(string.Empty, Source.Select(x => x.Value));
            return Regex.Replace(codedText, @"\s+", " ").Trim();
        }

        return string.Join(string.Empty, Source.Select(x => x.Value));
    }

    /// <summary>
    /// Get the target text including tags
    /// </summary>
    /// <returns>The target including tags</returns>
    public string GetTarget()
    {
        if (CodeType.HasValue && CodeType.Value == Enums.CodeType.Html)
        {
            var codedText = string.Join(string.Empty, Target.Select(x => x.Value));
            return Regex.Replace(codedText, @"\s+", " ").Trim();
        }

        return string.Join(string.Empty, Target.Select(x => x.Value));
    }

    /// <summary>
    /// Set the target text 
    /// </summary>
    /// <returns>The text including tags</returns>
    public void SetTarget(string content)
    {
        if (CodeType.HasValue && CodeType.Value == Enums.CodeType.Html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            Target = ConvertToInlineTags(HtmlContentCoder.BuildTextParts(doc.DocumentNode.ChildNodes));
        }
        else
        {
            Target = [new() { Value = content }];
        }
    }

    private List<TextPart> ConvertToInlineTags(List<TextPart> codedParts)
    {
        var parts = new List<TextPart>();
        var dictionary = new Dictionary<StartCode, StartTag>();

        foreach (var part in codedParts)
        {
            switch (part)
            {
                case StartCode sc:
                    var startTag = new StartTag() { Value = sc.Value };
                    dictionary.Add(sc, startTag);
                    parts.Add(startTag);
                    break;
                case EndCode ec:
                    var endTag = new EndTag() { Value = ec.Value };
                    if (ec.StartCode != null)
                    {
                        var coupledStartTag = dictionary[ec.StartCode];
                        endTag.StartTag = coupledStartTag;
                        if (coupledStartTag is not null) coupledStartTag.EndTag = endTag;
                    }
                    parts.Add(endTag);
                    break;
                case InlineCode inline:
                    var inlineTag = new InlineTag() { Value = inline.Value };
                    parts.Add(inlineTag);
                    break;
                default:
                    parts.Add(new TextPart() { Value = part.Value });
                    break;
            }

        }
        return parts;
    }

}