using Blackbird.Filters.Transformations;
using System.Text.RegularExpressions;

namespace Blackbird.Filters.Content;

public class TextUnit(string reference)
{
    /// <summary>
    /// The reference to the location of this text unit in the original file depending on the original format.
    /// </summary>
    public string Reference { get; } = reference;

    /// <summary>
    /// All the individual parts. Concatenating all the part values will result in the plain text unit.
    /// </summary>
    public List<TextPart> Parts { get; set; } = [];

    /// <summary>
    /// Get the plain text without any tags.
    /// </summary>
    /// <param name="withSemanticSpaces">Default true. If true, tags will be replaced by spaces and all additional spaces are trimmed.</param>
    /// <returns>The plain text without any tags</returns>
    public string GetPlainText(bool withSemanticSpaces = true)
    {
        if (withSemanticSpaces)
        {
            var partsWithTagsAsSpaces = string.Join(string.Empty, Parts.Select(x => x is InlineCode || x is InlineTag ? " " : x.Value));
            return Regex.Replace(partsWithTagsAsSpaces, @"\s+", " ").Trim();
        }

        return string.Join(string.Empty, Parts.Where(x => x is not InlineCode && x is not InlineTag).Select(x => x.Value));
    }

    /// <summary>
    /// Get the text including tags
    /// </summary>
    /// /// <param name="trimWhitespace">Default true. If true, all additional spaces and line breaks that wrap text are trimmed.</param>
    /// <returns>The text including tags</returns>
    public string GetCodedText(bool trimWhitespace = true)
    {
        if (trimWhitespace)
        {
            var codedText = string.Join(string.Empty, Parts.Select(x => x.Value));
            return Regex.Replace(codedText, @"\s+", " ").Trim();
        }

        return string.Join(string.Empty, Parts.Select(x => x.Value));


    }
}
