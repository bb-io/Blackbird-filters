namespace Blackbird.Filters.Enums;

public enum SegmentState
{
    Initial,
    Translated,
    Reviewed,
    Final,
}

public static class SegmentStateHelper
{
    private static readonly Dictionary<SegmentState, string> _enumToString = new()
    {
        { SegmentState.Initial, "initial" },
        { SegmentState.Translated, "translated" },
        { SegmentState.Reviewed, "reviewed" },
        { SegmentState.Final, "final" },
    };

    private static readonly Dictionary<string, SegmentState> _stringToEnum = _enumToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string Serialize(this SegmentState value)
    {
        return _enumToString.TryGetValue(value, out var s) ? s : value.ToString();
    }

    public static SegmentState? ToSegmentState(this string value)
    {
        return _stringToEnum.TryGetValue(value, out var stringValue) ? stringValue : null;
    }
}