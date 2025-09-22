namespace Blackbird.Filters.Transformations;
public class LineElement
{
    public string Value { get; set; } = string.Empty;

    public virtual string Render() => Value;
}
