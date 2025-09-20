namespace Blackbird.Filters.Content.Tags;

public class EndCode : InlineCode
{
    public StartCode? StartCode { get; set; }
    public override string Render() => $"</{FormatStyle.GetPartialTag()}>";
}
