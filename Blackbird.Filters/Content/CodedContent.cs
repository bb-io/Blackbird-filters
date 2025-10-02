using Blackbird.Filters.Coders;
using Blackbird.Filters.Content.Tags;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Shared;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Tags;
using Blackbird.Filters.Xliff.Xliff1;
using Blackbird.Filters.Xliff.Xliff2;
using System.Net.Mime;

namespace Blackbird.Filters.Content;
public class CodedContent
{
    internal CodedContent(string fileName, string mediaType, string fileContent) 
    {
        OriginalName = fileName;
        OriginalMediaType = mediaType;
        Original = fileContent;
    }

    /// <summary>
    /// The original file name.
    /// </summary>
    public string OriginalName { get; set; }

    /// <summary>
    /// The original media type name.
    /// </summary>
    public string OriginalMediaType { get; internal set; }

    /// <summary>
    /// The original file in plain text.
    /// </summary>
    public string Original { get; internal set; }

    /// <summary>
    /// Extracted units of content from the original file.
    /// </summary>
    public List<TextUnit> TextUnits { get; internal set; } = [];

    /// <summary>
    /// The language (code) represented in this file
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// A reference to where this content lives in the real world.
    /// </summary>
    public SystemReference SystemReference { get; set; } = new SystemReference();

    /// <summary>
    /// Provenance information of the content
    /// </summary>
    public Provenance Provenance { get; set; } = new Provenance();

    public Transformation CreateTransformation(string? targetLanguage = null)
    {
        var contentCoder = ContentCoderFactory.FromMediaType(OriginalMediaType);
        var transformation = new Transformation(Language, targetLanguage) 
        { 
            Original = Original, 
            OriginalName = OriginalName, 
            OriginalMediaType = OriginalMediaType,
            SourceSystemReference = SystemReference,
            Provenance = Provenance,
        };

        var unitReferences = new Dictionary<InlineTag, List<TextUnit>>();
        var unitsDictionary = new Dictionary<TextUnit, Unit>();
        var groupsDictionary = new Dictionary<string, Group>();

        Unit CreateUnit(TextUnit textUnit, string? id = null)
        {
            var unit = new Unit(contentCoder) 
            { 
                Name = textUnit.Reference, 
                Id = id, 
                FormatStyle = textUnit.FormatStyle,
                SizeRestrictions = textUnit.SizeRestrictions,
            };

            unitsDictionary[textUnit] = unit;
            var parts = new List<LineElement>();

            var dictionary = new Dictionary<StartCode, StartTag>();

            foreach (var part in textUnit.Parts)
            {
                switch (part)
                {
                    case StartCode sc:
                        var startTag = new StartTag() { Value = sc.Value, FormatStyle = sc.FormatStyle };
                        dictionary.Add(sc, startTag);
                        unitReferences[startTag] = sc.UnitReferences;
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
                        unitReferences[endTag] = ec.UnitReferences;
                        parts.Add(endTag);
                        break;
                    case InlineCode inline:
                        var inlineTag = new InlineTag() { Value = inline.Value, FormatStyle = inline.FormatStyle };
                        unitReferences[inlineTag] = inline.UnitReferences;
                        parts.Add(inlineTag);
                        break;
                    default:
                        parts.Add(new LineElement() { Value = part.Value });
                        break;
                }
            }

            unit.Segments = [new Segment(contentCoder) { Source = parts, Ignorable = parts.All(x => string.IsNullOrWhiteSpace(x.Value)) }];
            return unit;
        }

        foreach(var textUnit in TextUnits)
        {
            var unit = CreateUnit(textUnit);
            if (textUnit.Key is null)
            {
                transformation.Children.Add(unit);
            }
            else
            {
                if (TextUnits.Where(x => x.Key == textUnit.Key).Count() == 1)
                {
                    unit.Key = textUnit.Key;
                    transformation.Children.Add(unit);
                }
                else
                {
                    if (groupsDictionary.ContainsKey(textUnit.Key))
                    {
                        groupsDictionary[textUnit.Key].Children.Add(unit);
                    } 
                    else
                    {
                        var group = new Group { Key = textUnit.Key };
                        group.Children.Add(unit);
                        groupsDictionary[textUnit.Key] = group;
                        transformation.Children.Add(group);
                    }
                }
            }            
        }

        foreach (var unitRefrence in unitReferences)
        {
            unitRefrence.Key.UnitReferences = unitRefrence.Value.Select(x => unitsDictionary[x]).ToList();
        }

        return transformation;
    }

    /// <summary>
    /// Get the plaintext of originally coded content. Removes all tags and inline codes.
    /// </summary>
    /// <returns>The plaintext</returns>
    public string GetPlaintext()
    {
        return string.Join(Environment.NewLine, TextUnits.Select(x => x.GetPlainText()));
    }

    public string Serialize()
    {
        return ContentCoderFactory.FromMediaType(OriginalMediaType).Serialize(this);
    }

    public static CodedContent Parse(string content, string fileName)
    {
        if (Xliff2Serializer.IsXliff2(content))
        {
            var result = Xliff2Serializer.Deserialize(content).Target();
            if (result.TextUnits.All(x => string.IsNullOrEmpty(x.GetPlainText())))
            {
                return Xliff2Serializer.Deserialize(content).Source();
            }
            
            return result;
        }
        else if (Xliff1Serializer.IsXliff1(content))
        {
            var result = Xliff1Serializer.Deserialize(content).Target();
            if (result.TextUnits.All(x => string.IsNullOrEmpty(x.GetPlainText())))
            {
                return Xliff1Serializer.Deserialize(content).Source();
            }
            
            return result;
        }

        return ContentCoderFactory.FromContent(content).Deserialize(content, fileName);
    }

    public static async Task<CodedContent> Parse(Stream content, string fileName) => Parse(await content.ReadString(), fileName);
}