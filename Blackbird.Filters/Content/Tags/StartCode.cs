namespace Blackbird.Filters.Content.Tags;

public class StartCode : InlineCode
{
    public EndCode? EndCode { get; set; }

    public override string Render()
    {
        var partial = FormatStyle.GetPartialTag();
        if (string.IsNullOrEmpty(partial)) return string.Empty;
        return $"<{FormatStyle.GetPartialTag()}>";
    }
}
