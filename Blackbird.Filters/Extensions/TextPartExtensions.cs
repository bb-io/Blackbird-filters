using Blackbird.Filters.Content;
using Blackbird.Filters.Content.Tags;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Tags;

namespace Blackbird.Filters.Extensions;
public static class TextPartExtensions
{
    public static List<LineElement> ConvertToInlineTags(this List<TextPart> codedParts)
    {
        var parts = new List<LineElement>();
        var dictionary = new Dictionary<StartCode, StartTag>();

        foreach (var part in codedParts)
        {
            switch (part)
            {
                case StartCode sc:
                    var startTag = new StartTag() { Value = sc.Value, FormatStyle = sc.FormatStyle };
                    dictionary.Add(sc, startTag);
                    parts.Add(startTag);
                    break;
                case EndCode ec:
                    var endTag = new EndTag() { Value = ec.Value, FormatStyle = ec.FormatStyle };
                    if (ec.StartCode != null)
                    {
                        var coupledStartTag = dictionary[ec.StartCode];
                        endTag.StartTag = coupledStartTag;
                        if (coupledStartTag is not null) coupledStartTag.EndTag = endTag;
                    }
                    parts.Add(endTag);
                    break;
                case InlineCode inline:
                    var inlineTag = new InlineTag() { Value = inline.Value, FormatStyle = inline.FormatStyle };
                    parts.Add(inlineTag);
                    break;
                default:
                    parts.Add(new LineElement() { Value = part.Value });
                    break;
            }
        }
        return parts;
    }
}
