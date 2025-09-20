using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace Blackbird.Filters.Enums;
public enum HtmlTag
{
    [EnumMember(Value = "a")] Anchor,
    [EnumMember(Value = "b")] Bold,
    [EnumMember(Value = "bdo")] Bdo,
    [EnumMember(Value = "big")] Big,
    [EnumMember(Value = "blockquote")] Blockquote, 
    [EnumMember(Value = "body")] Body,
    [EnumMember(Value = "br")] LineBreak,
    [EnumMember(Value = "button")] Button,
    [EnumMember(Value = "caption")] Caption,
    [EnumMember(Value = "center")] Center, 
    [EnumMember(Value = "cite")] Cite, 
    [EnumMember(Value = "code")] Code,
    [EnumMember(Value = "col")] TableColumn,
    [EnumMember(Value = "colgroup")] TableColumnGroup, 
    [EnumMember(Value = "dd")] DefinitionDescription, 
    [EnumMember(Value = "del")] Deleted,
    [EnumMember(Value = "div")] Div,
    [EnumMember(Value = "dl")] DefinitionList, 
    [EnumMember(Value = "dt")] DefinitionTerm, 
    [EnumMember(Value = "em")] Emphasis,
    [EnumMember(Value = "h1")] H1,
    [EnumMember(Value = "h2")] H2,
    [EnumMember(Value = "h3")] H3,
    [EnumMember(Value = "h4")] H4,
    [EnumMember(Value = "h5")] H5,
    [EnumMember(Value = "h6")] H6,
    [EnumMember(Value = "head")] Head,
    [EnumMember(Value = "hr")] HorizontalRule,
    [EnumMember(Value = "html")] Html,
    [EnumMember(Value = "i")] Italic,
    [EnumMember(Value = "img")] Image,
    [EnumMember(Value = "label")] Label,
    [EnumMember(Value = "legend")] Legend,
    [EnumMember(Value = "li")] ListItem,
    [EnumMember(Value = "ol")] OrderedList,
    [EnumMember(Value = "p")] Paragraph,
    [EnumMember(Value = "pre")] Pre,
    [EnumMember(Value = "q")] ShortInlineQuotation,
    [EnumMember(Value = "s")] Strikethrough,
    [EnumMember(Value = "samp")] Sample,
    [EnumMember(Value = "select")] Select,
    [EnumMember(Value = "small")] Small,
    [EnumMember(Value = "span")] Span,
    [EnumMember(Value = "strike")] Strike,
    [EnumMember(Value = "strong")] Strong,
    [EnumMember(Value = "sub")] Subscript,
    [EnumMember(Value = "sup")] Superscript,
    [EnumMember(Value = "table")] Table, 
    [EnumMember(Value = "tbody")] TableBody,
    [EnumMember(Value = "td")] TableData,
    [EnumMember(Value = "tfoot")] TableFooter,
    [EnumMember(Value = "th")] TableHeaderCell,
    [EnumMember(Value = "thead")] TableHeader, 
    [EnumMember(Value = "title")] Title,
    [EnumMember(Value = "tr")] TableRow,
    [EnumMember(Value = "tt")] MonospacedText,
    [EnumMember(Value = "u")] Underline,
    [EnumMember(Value = "ul")] UnorderedList
}
public static class HtmlTagExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> EnumToStringCache = new();
    private static readonly Lazy<Dictionary<string, HtmlTag>> StringToEnum =
        new(() => Enum.GetValues(typeof(HtmlTag))
                      .Cast<HtmlTag>()
                      .ToDictionary(
                          e => e.GetEnumMemberValue(),
                          e => e,
                          StringComparer.OrdinalIgnoreCase));

    public static string ToTag(this HtmlTag tag) =>
        EnumToStringCache.GetOrAdd(tag, e => e.GetEnumMemberValue());

    public static bool TryParseTag(string? tag, out HtmlTag value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return StringToEnum.Value.TryGetValue(tag.Trim(), out value);
    }

    public static HtmlTag? ParseTag(string tag) => TryParseTag(tag, out var v) ? v : null;

    private static string GetEnumMemberValue(this Enum e)
    {
        var type = e.GetType();
        var name = Enum.GetName(type, e) ?? throw new ArgumentException("Invalid enum value.", nameof(e));
        var field = type.GetField(name)!;
        var attr = field.GetCustomAttribute<EnumMemberAttribute>();
        return attr?.Value ?? name;
    }
}