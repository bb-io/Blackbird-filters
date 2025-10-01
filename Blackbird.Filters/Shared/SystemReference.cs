namespace Blackbird.Filters.Shared;
public class SystemReference
{
    /// <summary>
    /// A unique identifier to the content in the system it lives in.
    /// </summary>
    public string? ContentId { get; set; }

    /// <summary>
    /// The name this content has in the system
    /// </summary>
    public string? ContentName { get; set; }

    /// <summary>
    /// A URL to where the content can be edited
    /// </summary>
    public string? AdminUrl { get; set; }

    /// <summary>
    /// A URL to where the content can be viewed
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// A human readable name of what system this content is stored in
    /// </summary>
    public string? SystemName { get; set; }

    /// <summary>
    /// A unique identifier of the system
    /// </summary>
    public string? SystemRef { get; set; }
}
