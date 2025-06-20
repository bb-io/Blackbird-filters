namespace Blackbird.Filters.Enums;
public enum Reorder
{
    Yes,
    FirstNo,
    No,
}

public static class ReorderHelper
{
    private static readonly Dictionary<Reorder, string> _enumToString = new()
    {
        { Reorder.Yes, "yes" },
        { Reorder.FirstNo, "firstNo" },
        { Reorder.No, "no" },
    };

    private static readonly Dictionary<string, Reorder> _stringToEnum = _enumToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string Serialize(this Reorder value)
    {
        return _enumToString.TryGetValue(value, out var s) ? s : value.ToString();
    }

    public static Reorder? ToReorder(this string value)
    {
        return _stringToEnum.TryGetValue(value, out var stringValue) ? stringValue : null;
    }
}
