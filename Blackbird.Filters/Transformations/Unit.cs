using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Interfaces;

namespace Blackbird.Filters.Transformations;

public class Unit(IContentCoder coder) : UnitGrouping
{
    public List<Segment> Segments { get; set; } = [];
    public IContentCoder ContentCoder { get; set; } = coder;
    public SegmentState State => Segments.Select(x => x.State ?? SegmentState.Initial).Min();
    public bool IsInitial => State == SegmentState.Initial;

    public TextUnit GetSource()
    {
        return new TextUnit(Name!, ContentCoder)
        {
            Parts = [.. Segments.OrderBy(x => x.Order).SelectMany(x => x.Source.ConvertToTextParts())],
            FormatStyle = FormatStyle,
        };
    }

    public TextUnit GetTarget()
    {
        return new TextUnit(Name!, ContentCoder)
        {
            Parts = [.. Segments.OrderBy(x => x.Order).SelectMany(x => x.Target.ConvertToTextParts())],
            FormatStyle = FormatStyle,
        };
    }
}