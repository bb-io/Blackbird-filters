namespace Blackbird.Filters.Enums;
public enum InlineTagType
{
    Formatting,
    Ui,
    Quote,
    Link,
    Image,
    Other
}

public static class InlineTagTypeHelper
{
    private static readonly Dictionary<InlineTagType, string> _enumToString = new()
    {
        { InlineTagType.Formatting, "fmt" },
        { InlineTagType.Ui, "ui" },
        { InlineTagType.Quote, "quote" },
        { InlineTagType.Link, "link" },
        { InlineTagType.Image, "image" },
        { InlineTagType.Other, "other" },
    };

    private static readonly Dictionary<string, InlineTagType> _stringToEnum = _enumToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string Serialize(this InlineTagType value)
    {
        return _enumToString.TryGetValue(value, out var s) ? s : value.ToString();
    }

    public static InlineTagType? ToInlineTagType(this string value)
    {
        return _stringToEnum.TryGetValue(value, out var stringValue) ? stringValue : null;
    }
}