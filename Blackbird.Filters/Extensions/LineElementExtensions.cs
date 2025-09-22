using Blackbird.Filters.Content;
using Blackbird.Filters.Content.Tags;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;
using Blackbird.Filters.Transformations.Tags;

namespace Blackbird.Filters.Extensions;
public static class LineElementExtensions
{
    public static List<TextPart> ConvertToTextParts(this List<LineElement> lineElements)
    {
        var parts = new List<TextPart>();
        var dictionary = new Dictionary<StartTag, StartCode>();

        foreach (var part in lineElements)
        {
            switch (part)
            {
                case StartTag sc:
                    var startCode = new StartCode() { Value = sc.Value, FormatStyle = sc.FormatStyle };
                    dictionary.Add(sc, startCode);
                    parts.Add(startCode);
                    break;
                case EndTag ec:
                    var endCode = new EndCode() { Value = ec.Value, FormatStyle = ec.FormatStyle };
                    if (ec.StartTag != null)
                    {
                        var coupledStartTag = dictionary[ec.StartTag];
                        endCode.StartCode = coupledStartTag;
                        if (coupledStartTag is not null) coupledStartTag.EndCode = endCode;
                    }
                    parts.Add(endCode);
                    break;
                case InlineTag inline:
                    var inlineCode = new InlineCode() { Value = inline.Value, FormatStyle = inline.FormatStyle };
                    parts.Add(inlineCode);
                    break;
                case AnnotationStart anStart:
                    break;
                case AnnotationEnd anEnd:
                    break;
                default:
                    parts.Add(new TextPart() { Value = part.Value });
                    break;
            }
        }
        return parts;
    }
}
