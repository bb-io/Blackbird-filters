namespace Blackbird.Filters.Content;

public class InlineCode : TextPart
{
    /// <summary>
    /// References to units that are actually rendered in the place of this inline code.
    /// </summary>
    public List<TextUnit> UnitReferences { get; set; } = [];
}
