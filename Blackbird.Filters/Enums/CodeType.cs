namespace Blackbird.Filters.Enums;

public enum CodeType
{
    PlainText,
    Html,
}

public static class CodeTypeHelper
{
    private static readonly Dictionary<CodeType, string> _enumToString = new()
    {
        { CodeType.PlainText, "plain" },
        { CodeType.Html, "html" },
    };

    private static readonly Dictionary<string, CodeType> _stringToEnum = _enumToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string Serialize(this CodeType value)
    {
        return _enumToString.TryGetValue(value, out var s) ? s : value.ToString();
    }

    public static CodeType? ToCodeType(this string value)
    {
        return _stringToEnum.TryGetValue(value, out var stringValue) ? stringValue : null;
    }
}
