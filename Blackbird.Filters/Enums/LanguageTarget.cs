namespace Blackbird.Filters.Enums;
public enum LanguageTarget
{
    Source,
    Target
}

public static class LanguageTargetHelper
{
    private static readonly Dictionary<LanguageTarget, string> _enumToString = new()
    {
        { LanguageTarget.Source, "source" },
        { LanguageTarget.Target, "target" },
    };

    private static readonly Dictionary<string, LanguageTarget> _stringToEnum = _enumToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string Serialize(this LanguageTarget value)
    {
        return _enumToString.TryGetValue(value, out var s) ? s : value.ToString();
    }

    public static LanguageTarget? ToLanguageTarget(this string value)
    {
        return _stringToEnum.TryGetValue(value, out var stringValue) ? stringValue : null;
    }
}