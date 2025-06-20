namespace Blackbird.Filters.Enums;
public enum Direction
{
    RightToLeft,
    LeftToRight
}

public static class DirectionHelper
{
    private static readonly Dictionary<Direction, string> _enumToString = new()
    {
        { Direction.RightToLeft, "rtl" },
        { Direction.LeftToRight, "ltr" },
    };

    private static readonly Dictionary<string, Direction> _stringToEnum = _enumToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string Serialize(this Direction value)
    {
        return _enumToString.TryGetValue(value, out var s) ? s : value.ToString();
    }

    public static Direction? ToDirection(this string value)
    {
        return _stringToEnum.TryGetValue(value, out var stringValue) ? stringValue : null;
    }
}