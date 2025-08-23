namespace Blackbird.Filters.Transformations;
public class Metadata(string type, string value)
{
    public List<string> Category { get; set; } = [];
    public string Type { get; set; } = type;
    public string Value { get; set; } = value;
    internal bool Global { get; set; } = false;
}