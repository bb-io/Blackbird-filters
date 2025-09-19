namespace Blackbird.Filters.Shared;
public class ProvenanceRecord : IEquatable<ProvenanceRecord>
{
    /// <summary>
    /// The name of the person
    /// </summary>
    public string? Person { get; set; }

    /// <summary>
    /// An IRI (preferable a URL) to the person
    /// </summary>
    public string? PersonReference { get; set; }

    /// <summary>
    /// The name of the organization
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// An IRI (preferable a URL) to the organization
    /// </summary>
    public string? OrganizationReference { get; set; }

    /// <summary>
    /// The name of the tool
    /// </summary>
    public string? Tool { get; set; }

    /// <summary>
    /// An IRI (preferable a URL) to the tool
    /// </summary>
    public string? ToolReference { get; set; }

    public bool Equals(ProvenanceRecord? other)
    {
        return Person == other?.Person && 
            PersonReference == other?.PersonReference && 
            Organization == other?.Organization && 
            OrganizationReference == other?.OrganizationReference && 
            Tool == other?.Tool && 
            ToolReference == other?.ToolReference;
    }

    internal bool IsEmpty()
    {
        return Person is null && PersonReference is null && Organization is null && OrganizationReference is null && Tool is null && ToolReference is null;
    }
}
