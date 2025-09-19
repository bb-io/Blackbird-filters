using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Shared;
using System.Xml.Linq;

namespace Blackbird.Filters.Transformations;

public abstract class Node
{
    public string? Id { get; internal set; }
    public bool? CanResegment { get; set; }
    public bool? Translate { get; set; }
    public Direction? SourceDirection { get; set; }
    public Direction? TargetDirection { get; set; }
    public List<Note> Notes { get; set; } = [];
    public List<XObject> Other { get; set; } = [];
    public List<Metadata> MetaData { get; set; } = [];
    public Quality Quality { get; set; } = new Quality();
    public Provenance Provenance { get; set; } = new Provenance();

    protected string? GetBlackbirdMetadata(string type)
    {
        return MetaData.FirstOrDefault(x => x.Category.Contains(Meta.Categories.Blackbird) && x.Type == type)?.Value;
    }

    protected void SetBlackbirdMetadata(string type, string? value)
    {
        var existing = MetaData.FirstOrDefault(x => x.Category.Contains(Meta.Categories.Blackbird) && x.Type == type);
        if (value is null && existing is not null)
        {
            MetaData.Remove(existing);
        }

        if (value is not null)
        {
            MetaData.Add(new Metadata(type, value) { Category = [Meta.Categories.Blackbird] });
        }
    }
}