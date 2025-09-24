using Blackbird.Filters.Interfaces;
using Blackbird.Filters.Shared;
using Blackbird.Filters.Transformations;

namespace Blackbird.Filters.Content;

public class TextUnit(string reference, IContentCoder coder)
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
    /// The coder that is responsible for serializing/deserializing this unit
    /// </summary>
    public IContentCoder ContentCoder { get; set; } = coder;

    /// <summary>
    /// Keys are used for change detection. Only changes among text units of the same key are considered. 
    /// Units within the same content can have the same keys. But keys should refer to unique content pieces in the document 
    /// or even the content environment.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Represents information for if this unit were to be rendered on a screen. Includes the HTML tag and potential attributes.
    /// </summary>
    public FormatStyle FormatStyle { get; set; } = new FormatStyle();

    /// <summary>
    /// Get the plain text without any tags.
    /// </summary>
    /// <returns>The plain text without any tags</returns>
    public string GetPlainText()
    {
        return string.Join(string.Empty, Parts.Where(x => x is not InlineCode).Select(x => x.Value));
    }

    /// <summary>
    /// Get a rendered HTML of the text. This uses the format style module in order to create a visual representation for browsers. It is not to be processed.
    /// </summary>
    /// <returns>An HTML formatted string</returns>
    public string GetRenderedText()
    {
        return FormatStyle.ToHtml(string.Join(string.Empty, Parts.Select(x => x.Render())), Reference);
    }

    /// <summary>
    /// Get the full text including tags
    /// </summary>
    /// <returns>The text including tags</returns>
    public string GetCodedText()
    {
        return string.Join(string.Empty, Parts.Select(x => x.Value));
    }

    /// <summary>
    /// Get normalized text that can be used for semantic comparison.
    /// </summary>
    /// <returns>Normalized text</returns>
    public string GetNormalizedText()
    {
        var codedText = GetCodedText();
        if (string.IsNullOrEmpty(codedText)) return codedText ?? string.Empty;
        return ContentCoder.NormalizeSegment(codedText);
    }

    /// <summary>
    /// Update the coded content with new/transformed content.
    /// </summary>
    /// <param name="content">The text (can include tags)</param>
    public void SetCodedText(string content)
    {
        Parts = ContentCoder.DeserializeSegment(content);
    }
}