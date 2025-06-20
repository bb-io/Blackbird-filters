using Blackbird.Filters.Enums;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;
public abstract class Node
{
    public string? Id { get; internal set; }
    public bool? CanResegment { get; set; }
    public bool? Translate { get; set; }
    public Direction? SourceDirection { get; set; }
    public Direction? TargetDirection { get; set; }
    public List<Note> Notes { get; set; } = [];
    public List<XObject> Other { get; set; } = [];
}
