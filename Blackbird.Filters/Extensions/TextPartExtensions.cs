using Blackbird.Filters.Content;
using Blackbird.Filters.Content.Tags;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Tags;

namespace Blackbird.Filters.Extensions;
public static class TextPartExtensions
{
    public static List<TextPart> ConvertToInlineTags(this List<TextPart> codedParts)
    {
        var parts = new List<TextPart>();
        var dictionary = new Dictionary<StartCode, StartTag>();

        foreach (var part in codedParts)
        {
            switch (part)
            {
                case StartCode sc:
                    var startTag = new StartTag() { Value = sc.Value };
                    dictionary.Add(sc, startTag);
                    parts.Add(startTag);
                    break;
                case EndCode ec:
                    var endTag = new EndTag() { Value = ec.Value };
                    if (ec.StartCode != null)
                    {
                        var coupledStartTag = dictionary[ec.StartCode];
                        endTag.StartTag = coupledStartTag;
                        if (coupledStartTag is not null) coupledStartTag.EndTag = endTag;
                    }
                    parts.Add(endTag);
                    break;
                case InlineCode inline:
                    var inlineTag = new InlineTag() { Value = inline.Value };
                    parts.Add(inlineTag);
                    break;
                default:
                    parts.Add(new TextPart() { Value = part.Value });
                    break;
            }

        }
        return parts;
    }
}
