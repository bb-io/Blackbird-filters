using System.Text;

namespace Blackbird.Filters.Extensions;
public static class StreamExtensions
{
    public static async Task<string> ReadString(this Stream stream)
    {
        byte[] bytes;
        await using (MemoryStream resultFileStream = new())
        {
            await stream.CopyToAsync(resultFileStream);
            bytes = resultFileStream.ToArray();
        }

        return Encoding.UTF8.GetString(bytes);
    }
}
