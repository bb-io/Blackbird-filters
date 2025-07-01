namespace Blackbird.Filters.Enums;

internal enum Target12State
{
    Final,
    NeedsAdaptation,
    NeedsL10n,
    NeedsReviewAdaptation,
    NeedsReviewL10n,
    NeedsReviewTranslation,
    NeedsTranslation,
    New,
    SignedOff,
    Translated
}

internal static class Target12StateHelper
{
    private static readonly Dictionary<Target12State, string> _enumToString = new()
    {
        { Target12State.Final, "final" },
        { Target12State.NeedsAdaptation, "needs-adaptation" },
        { Target12State.NeedsL10n, "needs-l10n" },
        { Target12State.NeedsReviewAdaptation, "needs-review-adaptation" },
        { Target12State.NeedsReviewL10n, "needs-review-l10n" },
        { Target12State.NeedsReviewTranslation, "needs-review-translation" },
        { Target12State.NeedsTranslation, "needs-translation" },
        { Target12State.New, "new" },
        { Target12State.SignedOff, "signed-off" },
        { Target12State.Translated, "translated" },
    };

    private static readonly Dictionary<string, Target12State> _stringToEnum = _enumToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string Serialize(this Target12State value)
    {
        return _enumToString.TryGetValue(value, out var s) ? s : value.ToString();
    }

    public static Target12State? ToTarget12State(this string value)
    {
        return _stringToEnum.TryGetValue(value, out var stringValue) ? stringValue : null;
    }

    public static SegmentState? ToSegmentState(this Target12State target12State)
    {
        switch (target12State)
        {
            case Target12State.New:
            case Target12State.NeedsTranslation:
                return SegmentState.Initial;
            case Target12State.Translated:
                return SegmentState.Translated;
            case Target12State.Final:
                return SegmentState.Final;
            case Target12State.SignedOff:
                return SegmentState.Reviewed;
            default:
                return SegmentState.Translated; // All other states are translated but needs review or adaptation
        }
    }
}