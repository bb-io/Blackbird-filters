using Blackbird.Filters.Enums;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;

public class Note(string text)
{
    public string? Id { get; set; }
    public LanguageTarget? LanguageTarget { get; set; }
    public string? Category { get; set; }
    public int? Priority { get; set; }
    public string? Reference { get; set; }
    public string Text { get; set; } = text;
    public List<XAttribute> Other { get; set; } = [];
}
