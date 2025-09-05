using Blackbird.Filters.Coders;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Interfaces;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace Blackbird.Filters.Content;

public class TextUnit(string reference, string? originalMediaType) : ITextContainer
{
    /// <summary>
    /// The reference to the location of this text unit in the original file depending on the original format.
    /// </summary>
    public string Reference { get; } = reference;

    /// <summary>
    /// All the individual parts. Concatenating all the part values will result in the plain text unit.
    /// </summary>
    public List<TextPart> Parts { get; set; } = [];

    public string? OriginalMediaType { get; set; } = originalMediaType;

    /// <summary>
    /// Keys are used for change detection. Only changes among text units of the same key are considered. 
    /// Units within the same content can have the same keys. But keys should refer to unique content pieces in the document 
    /// or even the content environment.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Get the plain text without any tags.
    /// </summary>
    /// <returns>The plain text without any tags</returns>
    public string GetPlainText()
    {
        return string.Join(string.Empty, Parts.Where(x => x is not InlineCode && x is not InlineTag).Select(x => x.Value));
    }

    /// <summary>
    /// Get the text including tags
    /// </summary>
    /// <returns>The text including tags</returns>
    public string GetCodedText()
    {
        return string.Join(string.Empty, Parts.Select(x => x.Value));
    }

    /// <summary>
    /// Update the coded content with new/transformed content.
    /// </summary>
    /// <param name="content">The text (can include tags)</param>
    public void SetCodedText(string content)
    {
        if (OriginalMediaType == MediaTypeNames.Text.Html)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(content);
                Parts = HtmlContentCoder.BuildTextParts(doc.DocumentNode.ChildNodes, Key);
            } catch (Exception)
            {
                Parts = [new() { Value = content }];
            }

        }
        else
        {
            Parts = [new() { Value = content }];
        }
    }

    public string GetTarget()
    {
        return GetCodedText();
    }

    public string? GetSource()
    {
        return null;
    }
}