using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Shared;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;

public class InlineTag : TextPart
{
    public string? Id { get; set; }
    public bool? CanCopy { get; set; }
    public bool? CanDelete { get; set; }
    public bool? CanOverlap { get; set; }
    public Reorder? CanReorder { get; set; }
    public string? CopyOf { get; set; }
    public Direction? Direction { get; set; }
    public Direction? DataDirection { get; set; }
    public string? DataId { get; set; }
    public string? Display { get; set; }
    public string? Equivalent { get; set; }
    public InlineTagType? Type { get; set; }
    public string? SubType { get; set; }
    public bool? Isolated { get; set; }
    public List<Unit> UnitReferences { get; set; } = [];
    public List<XAttribute> Other { get; set; } = [];
    public FormatStyle FormatStyle { get; set; } = new FormatStyle();

    public override string Render() => $"<{FormatStyle.GetPartialTag()}/>";
}
