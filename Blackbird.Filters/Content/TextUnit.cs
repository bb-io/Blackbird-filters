using Blackbird.Filters.Coders;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Blackbird.Filters.Content;

public class TextUnit(string reference, CodeType codeType)
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
    /// The type of coding of inline codes. Usually this depends on the file type.
    /// </summary>
    public CodeType CodeType { get; set; } = codeType;

    /// <summary>
    /// Get the plain text without any tags.
    /// </summary>
    /// <returns>The plain text without any tags</returns>
    public string GetPlainText()
    {
        // In HTML white spaces are not semantic.
        if (CodeType == CodeType.Html)
        {
            var partsWithTagsAsSpaces = string.Join(string.Empty, Parts.Select(x => x is InlineCode || x is InlineTag ? " " : x.Value));
            return Regex.Replace(partsWithTagsAsSpaces, @"\s+", " ").Trim();
        }

        return string.Join(string.Empty, Parts.Where(x => x is not InlineCode && x is not InlineTag).Select(x => x.Value));
    }

    /// <summary>
    /// Get the text including tags
    /// </summary>
    /// <returns>The text including tags</returns>
    public string GetCodedText()
    {
        if (CodeType == CodeType.Html)
        {
            var codedText = string.Join(string.Empty, Parts.Select(x => x.Value));
            return Regex.Replace(codedText, @"\s+", " ").Trim();
        }

        return string.Join(string.Empty, Parts.Select(x => x.Value));
    }

    /// <summary>
    /// Update the coded content with new/transformed content.
    /// </summary>
    /// <param name="content">The text (can include tags)</param>
    public void SetCodedText(string content)
    {
        if (CodeType == CodeType.Html)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(content);
                Parts = HtmlContentCoder.BuildTextParts(doc.DocumentNode.ChildNodes);
            } catch(Exception e)
            {
                Parts = [new() { Value = content }];
            }

        }
        else
        {
            Parts = [new() { Value = content }];
        }
    }
}
