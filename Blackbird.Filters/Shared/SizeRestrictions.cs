namespace Blackbird.Filters.Shared;
public class SizeRestrictions
{
    public int? MinimumSize { get; set; }
    public int? MaximumSize { get; set; }    
}

public static class SizeRestrictionHelper
{
    public static string? Serialize(SizeRestrictions sizeRestrictions)
    {
        if (sizeRestrictions.MaximumSize == null) return null;
        if (sizeRestrictions.MinimumSize == null) return $"{sizeRestrictions.MaximumSize}";
        return $"{sizeRestrictions.MinimumSize},{sizeRestrictions.MaximumSize}";
    }

    public static SizeRestrictions Deserialize(string? sizeString)
    {
        if (string.IsNullOrEmpty(sizeString)) return new SizeRestrictions();
        var split = sizeString.Split(',');

        try
        {
            if (split.Length > 1)
            {
                return new SizeRestrictions { MinimumSize = int.Parse(split[0]), MaximumSize = int.Parse(split[1]) };
            }
            return new SizeRestrictions { MaximumSize = int.Parse(sizeString) };
        }
        catch
        {
            return new SizeRestrictions();
        }
    }
}