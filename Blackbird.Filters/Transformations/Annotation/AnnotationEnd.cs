namespace Blackbird.Filters.Transformations.Annotation;

public class AnnotationEnd : LineElement
{
    public AnnotationStart? StartAnnotationReference { get; set; }
    public override string Render() => string.Empty;
}
