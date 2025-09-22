using Blackbird.Filters.Content;

namespace Blackbird.Filters.Interfaces;
public interface IContentCoder
{
    public IEnumerable<string> SupportedMediaTypes {  get; }
    public bool CanProcessContent(string content);

    public CodedContent Deserialize(string content, string fileName);
    public string Serialize(CodedContent content);

    public List<TextPart> DeserializeSegment(string segment);
    public string NormalizeSegment(string segment);
    
}
