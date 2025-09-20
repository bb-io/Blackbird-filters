using Blackbird.Filters.Content;
using Blackbird.Filters.Extensions;

namespace Blackbird.Filters.Transformations;

public class Unit : UnitGrouping
{
    public List<Segment> Segments { get; set; } = [];

    public TextUnit GetSource(string? originalMediaType = null)
    {
        return new TextUnit(Name!, originalMediaType)
        {
            Parts = [.. Segments.SelectMany(x => x.Source.ConvertToInlineTags())],
            FormatStyle = FormatStyle,
        };
    }

    public TextUnit GetTarget(string? originalMediaType = null)
    {
        return new TextUnit(Name!, originalMediaType)
        {
            Parts = [.. Segments.SelectMany(x => x.Target.ConvertToInlineTags())],
            FormatStyle = FormatStyle,
        };
    }
}