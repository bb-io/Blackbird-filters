using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Xliff.Xliff12;
using Blackbird.Filters.Xliff.Xliff2;
using System.Text;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;

public class Transformation(string? sourceLanguage, string? targetLanguage) : Node
{
    public string? SourceLanguage { get; set; } = sourceLanguage;
    public string? TargetLanguage { get; set; } = targetLanguage;
    public string? Original { get; set; }
    public string? OriginalReference { get; set; }
    public IEnumerable<XElement> SkeletonOther { get; set; } = [];
    public string? ExternalReference { get; set; }
    public List<Node> Children { get; set; } = [];
    public List<XObject> XliffOther { get; set; } = [];

    public IEnumerable<Unit> GetUnits()
    {
        foreach (var child in Children)
        {
            if (child is Unit unit)
            {
                yield return unit;
            }
            else if (child is Group group)
            {
                foreach (var subUnit in group.GetUnits())
                {
                    yield return subUnit;
                }
            }
            else if (child is Transformation transformation)
            {
                foreach (var subUnit in transformation.GetUnits())
                {
                    yield return subUnit;
                }
            }
        }
    }

    public IEnumerable<Segment> GetSegments()
    {
        foreach(var unit in GetUnits())
        {
            foreach(var segment in unit.Segments)
            {
                yield return segment;
            }
        }
    }

    public CodedContent Source()
    {
        if (Original is null) throw new Exception("Cannot convert to content, no original data found");
        var codedContent = new CodedContent() { Original = Original };
        foreach (var unit in GetUnits().Where(x => x.Name is not null))
        {
            var codeType = unit.Segments.FirstOrDefault()?.CodeType ?? CodeType.PlainText;
            var textUnit = new TextUnit(unit.Name!, codeType)
            {
                Parts = unit.Segments.SelectMany(x => x.Source).ToList()
            };
            codedContent.TextUnits.Add(textUnit);
        }
        return codedContent;
    }

    public CodedContent Target()
    {
        if (Original is null) throw new Exception("Cannot convert to content, no original data found");
        var codedContent = new CodedContent() { Original = Original };
        foreach(var unit in GetUnits().Where(x => x.Name is not null))
        {
            var codeType = unit.Segments.FirstOrDefault()?.CodeType ?? CodeType.PlainText;
            var textUnit = new TextUnit(unit.Name!, codeType)
            {
                Parts = unit.Segments.SelectMany(x => x.Target).ToList()
            };
            codedContent.TextUnits.Add(textUnit);
        }
        return codedContent;
    }

    public static Transformation Parse(string content)
    {
        if (Xliff2Serializer.IsXliff2(content))
        {
            return Xliff2Serializer.Deserialize(content);
        }
        else if (HtmlContentCoder.IsHtml(content))
        {
            return HtmlContentCoder.Deserialize(content).CreateTransformation();
        }
        if(Xliff12Serializer.IsXliff12(content))
        {
            return Xliff12Serializer.Deserialize(content);
        }
        else
        {
            throw new Exception("This file format is not supported by this library.");
        }
    }

    public static async Task<Transformation> Parse(Stream content)
    {
        byte[] bytes;
        await using (MemoryStream resultFileStream = new())
        {
            await content.CopyToAsync(resultFileStream);
            bytes = resultFileStream.ToArray();
        }
        return Parse(Encoding.UTF8.GetString(bytes));
    }

    public string Serialize()
    {
        return Xliff2Serializer.Serialize(this);
    }
}