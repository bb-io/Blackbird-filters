namespace Blackbird.Filters.Enums;
public enum Xliff2Version
{
    Xliff20,
    Xliff21,
    Xliff22,
}

public static  class Xliff2VersionHelper
{
    private static readonly Dictionary<Xliff2Version, string> _enumToString = new()
    {
        { Xliff2Version.Xliff20, "2.0" },
        { Xliff2Version.Xliff21, "2.1" },
        { Xliff2Version.Xliff22, "2.2" },
    };

    private static readonly Dictionary<string, Xliff2Version> _stringToEnum = _enumToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string Serialize(this Xliff2Version value)
    {
        return _enumToString.TryGetValue(value, out var s) ? s : value.ToString();
    }

    public static Xliff2Version? ToXliff2Version(this string value)
    {
        return _stringToEnum.TryGetValue(value, out var stringValue) ? stringValue : null;
    }
}
