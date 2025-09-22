using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Interfaces;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;
public class Segment(IContentCoder coder)
{
    public IContentCoder ContentCoder { get; set; } = coder;
    public List<LineElement> Source { get; set; } = [];
    public List<LineElement> Target { get; set; } = [];
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

    public bool IsIgnorbale => Ignorable.HasValue && Ignorable.Value;
    public bool IsInitial => State.HasValue ? State.Value == SegmentState.Initial : true;

    /// <summary>
    /// Get the source text including tags
    /// </summary>
    /// <returns>The source including tags</returns>
    public string GetSource()
    {
        return string.Join(string.Empty, Source.Select(x => x.Value));
    }

    /// <summary>
    /// Get the target text including tags
    /// </summary>
    /// <returns>The target including tags</returns>
    public string GetTarget()
    {
        return string.Join(string.Empty, Target.Select(x => x.Value));
    }

    /// <summary>
    /// Set the target text 
    /// </summary>
    /// <returns>The text including tags</returns>
    public void SetTarget(string content)
    {
        Target = ContentCoder.DeserializeSegment(content).ConvertToInlineTags();        
    }
}