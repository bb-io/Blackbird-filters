using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;
public class Transformation(string sourceLanguage, string? targetLanguage) : Node
{
    public string SourceLanguage { get; set; } = sourceLanguage;
    public string? TargetLanguage { get; set; } = targetLanguage;
    public string? Original { get; set; }
    public string? OriginalReference { get; set; }
    public IEnumerable<XElement> SkeletonOther { get; set; } = [];
    public CodeType CodeType { get; set; } = CodeType.PlainText;
    public string? ExternalReference { get; set; }
    public List<UnitGrouping> Children { get; set; } = [];

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
        }
    }

    public CodedContent Source()
    {
        var codedContent = new CodedContent() { Original = Original, CodeType = CodeType };
        foreach (var unit in GetUnits().Where(x => x.Name is not null))
        {
            var textUnit = new TextUnit(unit.Name!)
            {
                Parts = unit.Segments.SelectMany(x => x.Source).ToList()
            };
            codedContent.TextUnits.Add(textUnit);
        }
        return codedContent;
    }

    public CodedContent Target()
    {
        var codedContent = new CodedContent() { Original = Original, CodeType = CodeType };
        foreach(var unit in GetUnits().Where(x => x.Name is not null))
        {
            var textUnit = new TextUnit(unit.Name!)
            {
                Parts = unit.Segments.SelectMany(x => x.Target).ToList()
            };
            codedContent.TextUnits.Add(textUnit);
        }
        return codedContent;
    }


}