using Blackbird.Filters.Constants;

namespace Blackbird.Filters.Transformations;

public abstract class UnitGrouping : Node
{
    public string? Name { get; set; }
    public string? Type { get; set; }

    public string? Key
    {
        get => GetBlackbirdMetadata(Meta.Types.Key);
        internal set => SetBlackbirdMetadata(Meta.Types.Key, value);
    }
}