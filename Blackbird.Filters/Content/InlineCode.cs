using Blackbird.Filters.Shared;

namespace Blackbird.Filters.Content;

public class InlineCode : TextPart
{
    /// <summary>
    /// References to units that are actually rendered in the place of this inline code.
    /// </summary>
    public List<TextUnit> UnitReferences { get; set; } = [];

    /// <summary>
    /// Represents information for if this tag were to be rendered on a screen. Includes the HTML tag and potential attributes.
    /// </summary>
    public FormatStyle FormatStyle { get; set; } = new FormatStyle();

    public override string Render()
    {
        var partial = FormatStyle.GetPartialTag();
        if (string.IsNullOrEmpty(partial)) return string.Empty;
        return $"<{FormatStyle.GetPartialTag()}/>";
    }
}
