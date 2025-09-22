using Blackbird.Filters.Interfaces;

namespace Blackbird.Filters.Coders;
public static class ContentCoderFactory
{
    // NOTE: The order of content coders matters here and plaintext should always be last.
    public static List<IContentCoder> GetCoders() => [
        new HtmlContentCoder(),
        new PlaintextContentCoder(),
    ];

    public static IContentCoder FromMediaType(string mediaType)
    {
        var coder = GetCoders().FirstOrDefault(x => x.SupportedMediaTypes.Contains(mediaType));
        return coder is null
            ? throw new NotImplementedException($"The mediatype {mediaType} is currently not supported by this library")
            : coder;
    }

    public static IContentCoder FromContent(string content)
    {
        var coder = GetCoders().FirstOrDefault(x => x.CanProcessContent(content));
        return coder is null
            ? throw new NotImplementedException($"Could not detect any valid content type this library can process")
            : coder;
    }
}
