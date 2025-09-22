namespace Blackbird.Filters.Transformations.Tags;

public class EndTag : InlineTag
{
    public StartTag? StartTag { get; set; }

    public override string Render()
    {
        var partial = FormatStyle.GetPartialTag();
        if (string.IsNullOrEmpty(partial)) return string.Empty;
        return $"</{FormatStyle.GetPartialTag()}/>";
    }
}
