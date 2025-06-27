using Blackbird.Filters.Content;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations.Annotation;

public class AnnotationStart(bool wellFormed = false) : TextPart
{
    public string? Id { get; set; }
    public bool? Translate { get; set; }
    public string? Type { get; set; }
    public string? Ref { get; set; }
    public bool WellFormed { get; set; } = wellFormed;
    public string? AttributeValue { get; set; }
    public AnnotationEnd? EndAnnotationReference { get; set; }
    public List<XAttribute> Other { get; set; } = [];
}
