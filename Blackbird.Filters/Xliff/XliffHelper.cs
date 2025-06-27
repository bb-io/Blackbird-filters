using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff12;
using Blackbird.Filters.Xliff.Xliff2;

namespace Blackbird.Filters.Xliff;

/// <summary>
/// Helper class for working with different XLIFF versions
/// </summary>
public static class XliffHelper
{
    /// <summary>
    /// Parses XLIFF content and returns a transformation object
    /// </summary>
    public static Transformation ParseXliff(string content)
    {
        if (Xliff2Serializer.IsXliff2(content))
        {
            return Xliff2Serializer.Deserialize(content);
        }
        else if (Xliff12Serializer.IsXliff12(content))
        {
            return Xliff12Serializer.Deserialize(content);
        }
        else
        {
            throw new Exception("Unsupported XLIFF version. Only XLIFF 1.2 and 2.2 are supported.");
        }
    }

    /// <summary>
    /// Serializes a transformation to XLIFF format
    /// </summary>
    /// <param name="transformation">The transformation to serialize</param>
    /// <param name="version">XLIFF version to use (1.2 or 2.2)</param>
    /// <returns>XLIFF content as string</returns>
    public static string SerializeXliff(Transformation transformation, XliffVersion version = XliffVersion.Xliff22)
    {
        return version switch
        {
            XliffVersion.Xliff12 => Xliff12Serializer.Serialize(transformation),
            XliffVersion.Xliff22 => Xliff2Serializer.Serialize(transformation),
            _ => throw new ArgumentOutOfRangeException(nameof(version), "Unsupported XLIFF version")
        };
    }

    /// <summary>
    /// Determines the XLIFF version of content
    /// </summary>
    public static XliffVersion DetectXliffVersion(string content)
    {
        if (Xliff2Serializer.IsXliff2(content))
            return XliffVersion.Xliff22;
        else if (Xliff12Serializer.IsXliff12(content))
            return XliffVersion.Xliff12;
        else
            throw new Exception("Unsupported XLIFF version");
    }
}

public enum XliffVersion
{
    Xliff12,
    Xliff22
}
