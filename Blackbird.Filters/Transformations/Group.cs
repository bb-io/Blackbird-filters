namespace Blackbird.Filters.Transformations;
public class Group : UnitGrouping
{
    public List<UnitGrouping> Children { get; set; } = [];

    public IEnumerable<Unit> GetUnits()
    {
        foreach (var child in Children) 
        {
            if (child is Unit unit)
            {
                yield return unit;
            }
            else if (child is Group group)
            {
                foreach (var subUnit in group.GetUnits())
                {
                    yield return subUnit;
                }
            }
        }
    }
}
