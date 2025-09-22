using Blackbird.Filters.Content;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Interfaces;

namespace Blackbird.Filters.Transformations;

public class Unit(IContentCoder coder) : UnitGrouping
{
    public List<Segment> Segments { get; set; } = [];
    public IContentCoder ContentCoder { get; set; } = coder;

    public TextUnit GetSource()
    {
        return new TextUnit(Name!, ContentCoder)
        {
            Parts = [.. Segments.SelectMany(x => x.Source.ConvertToInlineTags())],
            FormatStyle = FormatStyle,
        };
    }

    public TextUnit GetTarget()
    {
        return new TextUnit(Name!, ContentCoder)
        {
            Parts = [.. Segments.SelectMany(x => x.Target.ConvertToInlineTags())],
            FormatStyle = FormatStyle,
        };
    }
}