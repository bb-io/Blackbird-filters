using Blackbird.Filters.Constants;

namespace Blackbird.Filters.Transformations;

public abstract class UnitGrouping : Node
{
    public string? Name { get; set; }
    public string? Type { get; set; }

    public virtual IEnumerable<Unit> GetUnits()
    {
        if (this is Unit unit)
        {
            return [unit];
        }
        else if (this is Group group)
        {
            return group.GetUnits();
        }
        else
        {
            return [];
        }
    }

    public string? Key
    {
        get => GetBlackbirdMetadata(Meta.Types.Key);
        internal set => SetBlackbirdMetadata(Meta.Types.Key, value);
    }
}