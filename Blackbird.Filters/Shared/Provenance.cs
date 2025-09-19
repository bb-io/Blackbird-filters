namespace Blackbird.Filters.Shared;
public class Provenance
{
    public ProvenanceRecord Translation { get; set; } = new();
    public ProvenanceRecord Review { get; set; } = new();

    internal bool IsEmpty()
    {
        return Translation.IsEmpty() && Review.IsEmpty();
    }
}
