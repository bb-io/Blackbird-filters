using Blackbird.Filters.Content;
using Blackbird.Filters.Interfaces;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace Blackbird.Filters.Coders;
public class PlaintextContentCoder : IContentCoder
{
    public IEnumerable<string> SupportedMediaTypes => [MediaTypeNames.Text.Plain];

    /// <summary>
    /// Turn any plaintext content into coded content.
    /// </summary>
    /// <param name="content">The plaintext string</param>
    /// <returns></returns>
    public CodedContent Deserialize(string content, string fileName)
    {
        var sentences = Regex
            .Split(content, @"\r\n|\n|\r", RegexOptions.Compiled)
            .ToList();

        var codedContent = new CodedContent(fileName, MediaTypeNames.Text.Plain, content)
        {
            TextUnits = sentences.Select(x => new TextUnit(string.Empty, this) { Parts = [new TextPart { Value = x}] }).ToList(),
        };

        return codedContent;
    }

    /// <summary>
    /// Turn plaintext coded content back into the original file string
    /// </summary>
    /// <param name="content">An plaintext coded content representation</param>
    /// <returns></returns>
    public string Serialize(CodedContent content)
    {
        var newLineCharacter = GetNewLineCharacter(content.Original);
        return string.Join(newLineCharacter, content.TextUnits.Select(x => x.GetCodedText()));
    }

    private static string GetNewLineCharacter(string content)
    {
        return content.Contains("\r\n") ? "\r\n"
            : content.Contains('\n') ? "\n"
            : content.Contains('\r') ? "\r"
            : Environment.NewLine;
    }


    /// <summary>
    /// Checks if the provided string is actually a plaintext file.
    /// </summary>
    /// <param name="content">The supposed plaintext content string</param>
    /// <returns>True if this is plaintext. False otherwise.</returns>
    public bool CanProcessContent(string content)
    {
        return !content.Contains('\0') && !content.Contains('\uFFFD');
    }

    public List<TextPart> DeserializeSegment(string segment)
    {
        return [new() { Value = segment }];
    }

    public string NormalizeSegment(string segment)
    {
        return segment;
    }
}