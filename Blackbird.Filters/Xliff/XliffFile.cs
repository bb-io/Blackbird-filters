using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using System.Xml.Linq;

namespace Blackbird.Filters.Xliff;
public class XliffFile(string sourceLanguage, Xliff2Version version)
{
    public string SourceLanguage { get; set; } = sourceLanguage;
    public string? TargetLanguage { get; set; }
    public Xliff2Version Version { get; set; } = version;
    public List<Transformation> Transformations { get; set; } = [];
    public List<XAttribute> Other { get; set; } = [];
}
