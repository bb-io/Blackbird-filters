namespace Blackbird.Filters.Content.Tags;

public class EndCode : InlineCode
{
    public StartCode? StartCode { get; set; }
    public override string Render()
    {
        var partial = FormatStyle.GetPartialTag();
        if (string.IsNullOrEmpty(partial)) return string.Empty;
        return $"</{FormatStyle.GetPartialTag()}/>";
    }
}
