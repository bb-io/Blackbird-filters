namespace Blackbird.Filters.Content.Tags;

public class StartCode : InlineCode
{
    public EndCode? EndCode { get; set; }

    public override string Render() => $"<{FormatStyle.GetPartialTag()}>";
}
