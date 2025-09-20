namespace Blackbird.Filters.Transformations.Tags;

public class EndTag : InlineTag
{
    public StartTag? StartTag { get; set; }

    public override string Render() => $"</{FormatStyle.GetPartialTag()}>";
}
