using Blackbird.Filters.Coders;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff2;
using System.Text;
using System.Xml.Linq;

namespace Blackbird.Filters.Xliff;
public class XliffFile(string sourceLanguage, Xliff2Version version)
{
    public string SourceLanguage { get; set; } = sourceLanguage;
    public string? TargetLanguage { get; set; }
    public Xliff2Version Version { get; set; } = version;
    public List<Transformation> Transformations { get; set; } = [];
    public List<XAttribute> Other { get; set; } = [];

    public IEnumerable<Segment> GetSegments()
    {
        foreach (var transformation in Transformations)
        {
            foreach (var segment in transformation.GetSegments())
            {
                yield return segment;
            }
        }
    }

    public static XliffFile? TryParse(string content, string sourceLanguage = "")
    {
        if (Xliff2Serializer.IsXliff2(content))
        {
            return Xliff2Serializer.Deserialize(content);
        }
        else if (HtmlContentCoder.IsHtml(content))
        {
            return new XliffFile(sourceLanguage, Xliff2Version.Xliff22)
            {
                Transformations = [HtmlContentCoder.Deserialize(content).CreateTransformation(sourceLanguage)]
            };
        }
        else
        {
            return null;
        }
    }

    public static async Task<XliffFile?> TryParse(Stream content)
    {
        byte[] bytes;
        await using (MemoryStream resultFileStream = new())
        {
            await content.CopyToAsync(resultFileStream);
            bytes = resultFileStream.ToArray();
        }
        return TryParse(Encoding.UTF8.GetString(bytes));
    }
}
