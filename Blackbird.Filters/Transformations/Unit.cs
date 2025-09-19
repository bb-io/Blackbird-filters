namespace Blackbird.Filters.Transformations;

public class Unit : UnitGrouping
{
    public List<Segment> Segments { get; set; } = [];

    public string GetSource() => string.Join(string.Empty, Segments.Select(x => x.GetSource()));

    public string GetTarget() => string.Join(string.Empty, Segments.Select(x => x.GetTarget()));
}