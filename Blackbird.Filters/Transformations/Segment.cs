using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;
public class Segment
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

    internal bool IsIgnorbale => Ignorable.HasValue && Ignorable.Value;

}