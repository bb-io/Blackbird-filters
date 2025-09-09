using Blackbird.Filters.Transformations.Modules;

namespace Blackbird.Filters.Transformations;

public class Unit : UnitGrouping
{
    public List<Segment> Segments { get; set; } = [];
    public ItsLocQuality? ItsLocQuality { get; set; }
}