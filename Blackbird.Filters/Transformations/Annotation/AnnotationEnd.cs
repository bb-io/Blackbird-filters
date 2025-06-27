using Blackbird.Filters.Content;

namespace Blackbird.Filters.Transformations.Annotation;

public class AnnotationEnd : TextPart
{
    public AnnotationStart? StartAnnotationReference { get; set; }
}
