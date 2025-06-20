namespace Blackbird.Filters.Transformations.Tags;
public class StartTag(bool wellFormed = false) : InlineTag
{
    public bool WellFormed { get; set; } = wellFormed;
    public EndTag? EndTag { get; set; }
}
