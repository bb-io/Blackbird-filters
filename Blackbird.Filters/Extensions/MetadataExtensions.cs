using Blackbird.Filters.Transformations;

namespace Blackbird.Filters.Extensions;

public static class MetadataExtensions
{

    public static string? Get(this List<Metadata> metadata, List<string> category, string type)
    {
        var existingMetadata = metadata.FirstOrDefault(x => x.Type == type && x.Category.ToHashSet().SetEquals(category));
        return existingMetadata?.Value;
    }

    public static string? Get(this List<Metadata> metadata, string type)
    {
        var existingMetadata = metadata.FirstOrDefault(x => x.Type == type);
        return existingMetadata?.Value;
    }


    public static void Set(this List<Metadata> metadata, List<string> category, string type, string value)
    {
        var existingMetadata = metadata.FirstOrDefault(x => x.Type == type && x.Category.ToHashSet().SetEquals(category));

        if (existingMetadata != null)
        {
            existingMetadata.Value = value;
        }
        else
        {
            metadata.Add(new Metadata(type, value) { Category = category ?? [] });
        }
    }

    public static void Set(this List<Metadata> metadata, string type, string value)
    {
        var existingMetadata = metadata.FirstOrDefault(x => x.Type == type);

        if (existingMetadata != null)
        {
            existingMetadata.Value = value;
        }
        else
        {
            metadata.Add(new Metadata(type, value));
        }
    }
}