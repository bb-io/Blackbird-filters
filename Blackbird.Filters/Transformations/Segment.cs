using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Blackbird.Filters.Transformations;
public class Segment()
{
    public List<TextPart> Source { get; set; } = [];
    public List<TextPart> Target { get; set; } = [];
    public string? Id { get; internal set; }
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
            Target = HtmlContentCoder.BuildTextParts(doc.DocumentNode.ChildNodes);
        }
        else
        {
            Target = [new() { Value = content }];
        }
    }

}