using Blackbird.Filters.Coders;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Content;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Xliff.Xliff1;
using Blackbird.Filters.Xliff.Xliff2;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;
public class Transformation(string? sourceLanguage, string? targetLanguage) : Node
{
    public string? SourceLanguage { get; set; } = sourceLanguage;
    public string? TargetLanguage { get; set; } = targetLanguage;
    public string? Original { get; set; }
    public string? OriginalReference { get; set; }
    public string? OriginalMediaType
    {
        get => GetBlackbirdMetadata(Meta.Types.OriginalMediaType);
        set => SetBlackbirdMetadata(Meta.Types.OriginalMediaType, value);        
    }

    public string? OriginalName
    {
        get => GetBlackbirdMetadata(Meta.Types.OriginalName);
        set => SetBlackbirdMetadata(Meta.Types.OriginalName, value);        
    }

    public IEnumerable<XElement> SkeletonOther { get; set; } = [];
    public string? ExternalReference { get; set; }
    public List<Node> Children { get; set; } = [];
    public List<XObject> XliffOther { get; set; } = [];
    public Quality Quality { get; set; } = new Quality();

    private string? _xliffFileName;
    /// <summary>
    /// Appropriate file name if this was saved as a serialized file.
    /// </summary>
    public string XliffFileName { get => _xliffFileName ?? ((OriginalName ?? OriginalReference ?? "transformation") + ".xlf"); set => _xliffFileName = value; }

    public string? UniqueSourceContentId
    {
        get => GetBlackbirdMetadata(Meta.Types.SourceUniqueContentId);
        set => SetBlackbirdMetadata(Meta.Types.SourceUniqueContentId, value);
    }

    public string? UniqueTargetContentId
    {
        get => GetBlackbirdMetadata(Meta.Types.TargetUniqueContentId);
        set => SetBlackbirdMetadata(Meta.Types.TargetUniqueContentId, value);
    }

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
        if (Original is null) throw new NullReferenceException("Cannot convert to content, no original data found");
        var codedContent = new CodedContent(OriginalName ?? OriginalReference ?? "transformation.txt", OriginalMediaType ?? MediaTypeNames.Text.Plain, Original);
        codedContent.Language = SourceLanguage;
        codedContent.UniqueContentId = UniqueSourceContentId;
        foreach (var unit in GetUnits().Where(x => x.Name is not null))
        {
            var textUnit = new TextUnit(unit.Name!, OriginalMediaType)
            {
                Parts = unit.Segments.SelectMany(x => x.Source).ToList()
            };
            codedContent.TextUnits.Add(textUnit);
        }
        return codedContent;
    }

    public CodedContent Target()
    {
        if (Original is null) throw new NullReferenceException("Cannot convert to content, no original data found");
        var codedContent = new CodedContent(OriginalName ?? OriginalReference ?? "transformation.txt", OriginalMediaType ?? MediaTypeNames.Text.Plain, Original);
        codedContent.Language = TargetLanguage;
        codedContent.UniqueContentId = UniqueTargetContentId;
        foreach (var unit in GetUnits().Where(x => x.Name is not null))
        {
            var textUnit = new TextUnit(unit.Name!, OriginalMediaType)
            {
                Parts = unit.Segments.SelectMany(x => x.Target).ToList()
            };
            codedContent.TextUnits.Add(textUnit);
        }
        return codedContent;
    }

    public static bool IsBilingual(string content)
    {
        return Xliff2Serializer.IsXliff2(content) || Xliff1Serializer.IsXliff1(content);
    }

    public static async Task<bool> IsBilingual(Stream content) => IsBilingual(await content.ReadString());

    public static Transformation Parse(string content, string fileName)
    {
        if (Xliff2Serializer.IsXliff2(content))
        {
            var transformation = Xliff2Serializer.Deserialize(content);
            transformation.XliffFileName = fileName;
            return transformation;
        }
        
        if(Xliff1Serializer.IsXliff1(content))
        {
            var transformation = Xliff1Serializer.Deserialize(content);
            transformation.XliffFileName = fileName;
            return transformation;
        }
        
        return CodedContent.Parse(content, fileName).CreateTransformation();
    }

    public static async Task<Transformation> Parse(Stream content, string fileName) => Parse(await content.ReadString(), fileName);

    public string Serialize()
    {
        return Xliff2Serializer.Serialize(this);
    }
}