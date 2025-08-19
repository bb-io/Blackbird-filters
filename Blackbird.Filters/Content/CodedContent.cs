using Blackbird.Filters.Coders;
using Blackbird.Filters.Content.Tags;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Tags;
using Blackbird.Filters.Xliff.Xliff2;
using System.Net.Mime;

namespace Blackbird.Filters.Content;

public class CodedContent
{
    internal CodedContent() { }

    /// <summary>
    /// The original file in plain text.
    /// </summary>
    public string Original { get; internal set; } = string.Empty;

    /// <summary>
    /// Extracted units of content from the original file.
    /// </summary>
    public List<TextUnit> TextUnits { get; internal set; } = [];

    public Transformation CreateTransformation(string? sourceLanguage = null, string? targetLanguage = null)
    {
        var transformation = new Transformation(sourceLanguage, targetLanguage) { Original = Original };
        var unitReferences = new Dictionary<InlineTag, List<TextUnit>>();
        var unitsDictionary = new Dictionary<TextUnit, Unit>();

        foreach(var textUnit in TextUnits)
        {
            var unit = new Unit() { Name = textUnit.Reference };
            unitsDictionary[textUnit] = unit;
            var parts = new List<TextPart>();

            var dictionary = new Dictionary<StartCode, StartTag>();

            foreach (var part in textUnit.Parts)
            {
                switch (part)
                {
                    case StartCode sc:
                        var startTag = new StartTag() { Value = sc.Value };
                        dictionary.Add(sc, startTag);
                        unitReferences[startTag] = sc.UnitReferences;
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
                        unitReferences[endTag] = ec.UnitReferences;
                        parts.Add(endTag);
                        break;
                    case InlineCode inline:
                        var inlineTag = new InlineTag() { Value = inline.Value };
                        unitReferences[inlineTag] = inline.UnitReferences;
                        parts.Add(inlineTag);
                        break;
                    default:
                        parts.Add(new TextPart() { Value = part.Value });
                        break;
                }
               
            }

            unit.Segments = [new Segment() { Source = parts, CodeType = textUnit.CodeType }];
            transformation.Children.Add(unit);
        }

        foreach (var unitRefrence in unitReferences)
        {
            unitRefrence.Key.UnitReferences = unitRefrence.Value.Select(x => unitsDictionary[x]).ToList();
        }

        return transformation;
    }

    public string Serialize()
    {
        if (OriginalMediaType == MediaTypeNames.Text.Html)
        {
            return HtmlContentCoder.Serialize(this);
        }

        if (OriginalMediaType == MediaTypeNames.Text.Plain)
        {
            return PlaintextContentCoder.Serialize(this);
        }

        throw new NotImplementedException($"The serializer for ${OriginalMediaType} is not implemented");
    }

    public static CodedContent Parse(string content, string fileName)
    {
        if (HtmlContentCoder.IsHtml(content))
        {
            return HtmlContentCoder.Deserialize(content, fileName);
        }
        else if (PlaintextContentCoder.IsPlaintext(content))
        {
            return PlaintextContentCoder.Deserialize(content, fileName);
        }
        else if (Xliff2Serializer.IsXliff2(content))
        {
            var result = Xliff2Serializer.Deserialize(content).Target();
            if (result.TextUnits.All(x => string.IsNullOrEmpty(x.GetPlainText())))
            {
                return Xliff2Serializer.Deserialize(content).Source();
            }
            return result;
        }
        else
        {
            throw new Exception("This file format is not supported by this library.");
        }
    }
}
