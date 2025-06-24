namespace Blackbird.Filters.Extensions;
public static class StringExtensions
{
    public static Stream ToStream(this string s)
    {
        MemoryStream memoryStream = new();
        StreamWriter streamWriter = new StreamWriter(memoryStream);
        streamWriter.Write(s);
        streamWriter.Flush();
        memoryStream.Position = 0L;
        return memoryStream;
    }
}
