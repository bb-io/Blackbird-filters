using Blackbird.Filters.Coders;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Shared;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;
using Blackbird.Filters.Transformations.Tags;
using System.Data;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Blackbird.Filters.Xliff.Xliff2;
public static class Xliff2Serializer
{
    public static readonly XNamespace MetaNs = "urn:oasis:names:tc:xliff:metadata:2.0";
    public static readonly XNamespace ItsNs = "http://www.w3.org/2005/11/its";
    public static readonly XNamespace FormatStyleNs = "urn:oasis:names:tc:xliff:fs:2.0";
    public static readonly XNamespace SizeRestrictionNs = "urn:oasis:names:tc:xliff:sizerestriction:2.0";

    private static readonly List<XName> CommonNodeLevelAttributes = [
        "id",
        "canResegment",
        "translate",
        "srcDir",
        "trgDir",
        ItsNs + "locQualityRatingScore",
        ItsNs + "locQualityRatingScoreThreshold",
        ItsNs + "locQualityRatingVote",
        ItsNs + "locQualityRatingVoteThreshold",
        ItsNs + "locQualityRatingProfileRef",
        ItsNs + "person",
        ItsNs + "personRef",
        ItsNs + "org",
        ItsNs + "orgRef",
        ItsNs + "tool",
        ItsNs + "toolRef",
        ItsNs + "revPerson",
        ItsNs + "revPersonRef",
        ItsNs + "revOrg",
        ItsNs + "revOrgRef",
        ItsNs + "revTool",
        ItsNs + "revToolRef",
        FormatStyleNs + "fs",
        FormatStyleNs + "subFs",
    ];

    public static Transformation Deserialize(string fileContent)
    {
        var xliffNode = GetRootNode(fileContent)
            ?? throw new NullReferenceException("No root node found in XLIFF content.");

        var ns = xliffNode.GetDefaultNamespace();
        var sourceLanguage = xliffNode.Get("srcLang");
        var targetLanguage = xliffNode.Get("trgLang");
        var version = xliffNode.GetXliffVersion("version");
        var whiteSpaceHandling = xliffNode.GetWhiteSpaceHandling();

        var globalMetadata = new List<Metadata>();
        var globalNotes = new List<Note>();

        List<Note> DeserializeNotes(XElement? node, bool global = false)
        {
            if (node == null) return [];
            var notes = node.Elements(ns + "note").Select(x => new Note(x.Value.Trim())
            {
                Id = x.Get("id"),
                Priority = x.GetInt("priority"),
                Category = x.Get("category")?.Replace(Meta.Categories.Global, string.Empty).NullIfEmpty(),
                LanguageTarget = x.GetLanguageTarget("appliesTo"),
                Other = x.Attributes().GetRemaining(["id", "priority", "category", "appliesTo"]),
                Global = global || (x.Get("category")?.Contains(Meta.Categories.Global) ?? false),
                // TODO: Implement reference property handling
            }).ToList();
            foreach(var note in notes.Where(x => x.Global))
            {
                if (!globalNotes.Any(x => x.Id == note.Id && x.Text == note.Text))
                {
                    globalNotes.Add(note);
                }
            }
            return notes.Where(x => !x.Global).ToList();
        }

        List<Metadata> DeserializeMetadata(XElement? node, List<string>? category = null, bool global = false)
        {
            var metadata = new List<Metadata>();
            if (node == null) return metadata;
            if (category == null) category = [];
            var categoryName = node.Get("category");
            if (categoryName is not null) category.Add(categoryName);
            metadata.AddRange(node.Elements(MetaNs + "metaGroup").SelectMany(x => DeserializeMetadata(x, category.ToList(), global)));            
            metadata.AddRange(node.Elements(MetaNs + "meta").Select(
                x => new Metadata(x.Get("type", Optionality.Required)!, x.Value) 
                {
                    Category = category.Where(x => x != Meta.Categories.Global).ToList(), 
                    Global = global || category.Contains(Meta.Categories.Global),
                }
                ));
            foreach (var meta in metadata.Where(x => x.Global))
            {
                if (!globalMetadata.Any(x => x.Value == meta.Value && x.Type == meta.Type))
                {
                    globalMetadata.Add(meta);
                }
            }
            return metadata.Where(x => !x.Global).ToList();
        }

        Quality DeserializeLocQuality(XElement node)
        {
            ArgumentNullException.ThrowIfNull(node);

            return new Quality()
            {
                Score = node.GetDouble(ItsNs + "locQualityRatingScore"),
                ScoreThreshold = node.GetDouble(ItsNs + "locQualityRatingScoreThreshold"),
                Votes = node.GetInt(ItsNs + "locQualityRatingVote"),
                VoteThreshold = node.GetInt(ItsNs + "locQualityRatingVoteThreshold"),
                ProfileReference = node.Get(ItsNs + "locQualityRatingProfileRef")
            };
        }

        FormatStyle DeserializeFormatStyle(XElement node)
        {
            var style = new FormatStyle();
            var fs = node.Get(FormatStyleNs + "fs");
            var subFs = node.Get(FormatStyleNs + "subFs");
            if (HtmlTagExtensions.TryParseTag(fs, out var parsed))
            {
                style.Tag = parsed;
                style.Attributes = SubFsHelper.ParseSubFsString(subFs);
            }
            return style;

        }

        SizeRestrictions DeserializeSizeRestrictions(XElement node)
        {
            return SizeRestrictionHelper.Deserialize(node.Get(SizeRestrictionNs + "sizeRestriction"));
        }

        Provenance DeserializeProvenance(XElement node)
        {
            ArgumentNullException.ThrowIfNull(node);

            var translation = new ProvenanceRecord
            {
                Person = node.Get(ItsNs + "person"),
                PersonReference = node.Get(ItsNs + "personRef"),
                Organization = node.Get(ItsNs + "org"),
                OrganizationReference = node.Get(ItsNs + "orgRef"),
                Tool = node.Get(ItsNs + "tool"),
                ToolReference = node.Get(ItsNs + "toolRef"),
            };

            var review = new ProvenanceRecord
            {
                Person = node.Get(ItsNs + "revPerson"),
                PersonReference = node.Get(ItsNs + "revPersonRef"),
                Organization = node.Get(ItsNs + "revOrg"),
                OrganizationReference = node.Get(ItsNs + "revOrgRef"),
                Tool = node.Get(ItsNs + "revTool"),
                ToolReference = node.Get(ItsNs + "revToolRef"),
            };

            return new Provenance { Translation = translation, Review = review };
        }

        Transformation DeserializeTransformation(XElement node)
        {
            whiteSpaceHandling = node.GetWhiteSpaceHandling(whiteSpaceHandling);
            var unitReferences = new Dictionary<InlineTag, List<string>>();

            var transformation = new Transformation(sourceLanguage, targetLanguage)
            {
                Id = node.Get("id", Optionality.Required),
                CanResegment = node.GetBool("canResegment"),
                Translate = node.GetBool("translate"),
                ExternalReference = node.Get("original"),
                SourceDirection = node.GetDirection("srcDir"),
                TargetDirection = node.GetDirection("trgDir"),
                Notes = DeserializeNotes(node.Element(ns + "notes")),
                MetaData = DeserializeMetadata(node.Element(MetaNs + "metadata")),
                OriginalReference = node.Element(ns + "skeleton")?.Get("href"),
                Quality = DeserializeLocQuality(node),
                Provenance = DeserializeProvenance(node),
                FormatStyle = DeserializeFormatStyle(node),
            };

            var contentCoder = string.IsNullOrEmpty(transformation.OriginalMediaType) ? new PlaintextContentCoder() : ContentCoderFactory.FromMediaType(transformation.OriginalMediaType);

            transformation.Other.AddRange(node.Elements().GetRemaining([
                ns + "skeleton",
                ns + "group",
                ns + "unit", 
                ns + "notes", 
                MetaNs + "metadata"]));
            transformation.Other.AddRange(node.Attributes().GetRemaining([ ..CommonNodeLevelAttributes, "original" ]));

            string? GetAndRemoveBlackbirdMetadata(string type)
            {
                var meta = transformation.MetaData.FirstOrDefault(x => x.Category.Contains(Meta.Categories.Blackbird) && x.Type == type);
                if (meta is null) return null;
                transformation.MetaData.Remove(meta);
                return meta.Value;
            }

            transformation.SourceSystemReference.ContentId = GetAndRemoveBlackbirdMetadata(Meta.Direction.Source + Meta.Types.ContentId);
            transformation.SourceSystemReference.ContentName = GetAndRemoveBlackbirdMetadata(Meta.Direction.Source + Meta.Types.ContentName);
            transformation.SourceSystemReference.AdminUrl = GetAndRemoveBlackbirdMetadata(Meta.Direction.Source + Meta.Types.AdminUrl);
            transformation.SourceSystemReference.PublicUrl = GetAndRemoveBlackbirdMetadata(Meta.Direction.Source + Meta.Types.PublicUrl);
            transformation.SourceSystemReference.SystemName = GetAndRemoveBlackbirdMetadata(Meta.Direction.Source + Meta.Types.SystemName);
            transformation.SourceSystemReference.SystemRef = GetAndRemoveBlackbirdMetadata(Meta.Direction.Source + Meta.Types.SystemRef);

            transformation.TargetSystemReference.ContentId = GetAndRemoveBlackbirdMetadata(Meta.Direction.Target + Meta.Types.ContentId);
            transformation.TargetSystemReference.ContentName = GetAndRemoveBlackbirdMetadata(Meta.Direction.Target + Meta.Types.ContentName);
            transformation.TargetSystemReference.AdminUrl = GetAndRemoveBlackbirdMetadata(Meta.Direction.Target + Meta.Types.AdminUrl);
            transformation.TargetSystemReference.PublicUrl = GetAndRemoveBlackbirdMetadata(Meta.Direction.Target + Meta.Types.PublicUrl);
            transformation.TargetSystemReference.SystemName = GetAndRemoveBlackbirdMetadata(Meta.Direction.Target + Meta.Types.SystemName);
            transformation.TargetSystemReference.SystemRef = GetAndRemoveBlackbirdMetadata(Meta.Direction.Target + Meta.Types.SystemRef);

            var skeleton = node.Element(ns + "skeleton");
            if (skeleton != null && skeleton.Nodes().Any())
            {
                transformation.Original = skeleton.Value;
                transformation.SkeletonOther = skeleton.Elements().Where(x => x.NodeType != XmlNodeType.Text);
            }

            Unit DeserializeUnit(XElement node)
            {
                whiteSpaceHandling = node.GetWhiteSpaceHandling(whiteSpaceHandling);
                var unit = new Unit(contentCoder)
                {
                    Id = node.Get("id", Optionality.Required),
                    Name = node.Get("name"),
                    CanResegment = node.GetBool("canResegment"),
                    Translate = node.GetBool("translate"),
                    SourceDirection = node.GetDirection("srcDir"),
                    TargetDirection = node.GetDirection("trgDir"),
                    Notes = DeserializeNotes(node.Element(ns + "notes")),
                    MetaData = DeserializeMetadata(node.Element(MetaNs + "metadata")),
                    Quality = DeserializeLocQuality(node),
                    Provenance = DeserializeProvenance(node),
                    FormatStyle = DeserializeFormatStyle(node),
                    SizeRestrictions = DeserializeSizeRestrictions(node),
                };

                unit.Other.AddRange(node.Elements().GetRemaining([
                    ns + "originalData", 
                    ns + "notes", 
                    ns + "segment", 
                    ns + "ignorable", 
                    MetaNs + "metadata"]));
                unit.Other.AddRange(node.Attributes().GetRemaining([.. CommonNodeLevelAttributes, "name", SizeRestrictionNs + "sizeRestriction"]));

                Dictionary<string, XElement> data = node.Element(ns + "originalData")?.Elements()?.ToDictionary(x => x.Get("id", Optionality.Required)!, x => x) ?? [];

                Segment DeserializeSegment(XElement node)
                {
                    var sourceNode = node.Element(ns + "source");
                    var targetNode = node.Element(ns + "target");
                    whiteSpaceHandling = node.GetWhiteSpaceHandling(whiteSpaceHandling);

                    if (sourceNode == null)
                    {
                        throw new ArgumentException($"Invalid XLIFF. {node.Name.LocalName} {node.BaseUri} does not have a source.");
                    }

                    List<LineElement> DeserializeLine(XElement node, WhiteSpaceHandling whiteSpaceHandling)
                    {
                        string SerializeCp(XElement element)
                        {
                            var hex = element.Get("hex", Optionality.Required);
                            var codePoint = int.Parse(hex!, System.Globalization.NumberStyles.HexNumber);
                            return char.ConvertFromUtf32(codePoint);
                        }

                        void AddPossibleSubflows(InlineTag tag, string? unitRef)
                        {
                            if (unitRef == null) return;
                            var ids = unitRef.Split(' ').ToList();
                            unitReferences[tag] = ids;
                        }

                        void SetTagValueFromDataRef(InlineTag tag, string? dataRef)
                        {
                            if (dataRef != null && data.TryGetValue(dataRef, out var dataElement))
                            {
                                tag.DataDirection = dataElement.GetDirection("dir");
                                tag.DataId = dataElement.Get("id", Optionality.Required);
                                var result = string.Empty;
                                foreach (var lineNode in dataElement.Nodes())
                                {
                                    if (lineNode is XText textNode && !string.IsNullOrWhiteSpace(textNode.Value))
                                    {
                                        var value = textNode.Value;
                                        if (dataElement.GetWhiteSpaceHandling(WhiteSpaceHandling.Preserve) != WhiteSpaceHandling.Preserve && lineNode.NodeType != XmlNodeType.CDATA) value = value.RemoveIdeFormatting();
                                        result += value;
                                        continue;
                                    }
                                    else if (lineNode is XElement element && element.Name == ns + "cp")
                                    {
                                        result += SerializeCp(element);
                                    }
                                }
                                tag.Value = result;
                            }
                        }

                        void SetCommonTagProperties(InlineTag tag, XElement element)
                        {
                            tag.Id = element.Get("id");
                            tag.CanCopy = element.GetBool("canCopy");
                            tag.CanDelete = element.GetBool("canDelete");
                            tag.CanOverlap = element.GetBool("canOverlap");
                            tag.CanReorder = element.GetReorder("canReorder");
                            tag.CopyOf = element.Get("copyOf");
                            tag.Direction = element.GetDirection("dir");
                            tag.SubType = element.Get("subType");
                            tag.Type = element.GetInlineType("type");
                            tag.Isolated = element.GetBool("isolated");
                            tag.FormatStyle = DeserializeFormatStyle(element);
                            tag.Other.AddRange(element.Attributes().GetRemaining([
                                "id",
                                "canCopy",
                                "canDelete",
                                "canOverlap",
                                "canReorder",
                                "copyOf",
                                "dir",
                                "isolated",
                                "subType",
                                "type",
                                "dataRefStart",
                                "subFlowsStart",
                                "dispStart",
                                "equivStart",
                                "dataRefEnd",
                                "subFlowsEnd",
                                "dispEnd",
                                "equivEnd",
                                "display",
                                "equiv",
                                "dataRef",
                                "subFlows",
                                "startRef",
                                FormatStyleNs + "fs",
                                FormatStyleNs + "subFs"
                                ]));
                        }

                        var parts = new List<LineElement>();
                        whiteSpaceHandling = node.GetWhiteSpaceHandling(whiteSpaceHandling);
                        foreach (var lineNode in node.Nodes())
                        {
                            if (lineNode is XText textNode)
                            {
                                var value = textNode.Value;
                                if (whiteSpaceHandling != WhiteSpaceHandling.Preserve && lineNode.NodeType != XmlNodeType.CDATA) value = value.RemoveIdeFormatting();
                                parts.Add(new LineElement { Value = value });
                                continue;
                            }

                            if (lineNode is not XElement) continue;
                            XElement element = (XElement)lineNode;

                            if (element.Name == ns + "cp")
                            {
                                parts.Add(new LineElement { Value = SerializeCp(element) });
                                continue;
                            }
                            else if (element.Name == ns + "mrk" || element.Name == ns + "sm")
                            {
                                var annotation = new AnnotationStart(element.Name == ns + "mrk")
                                {
                                    Id = element.Get("id"),
                                    Translate = element.GetBool("translate"),
                                    Type = element.Get("type"),
                                    Ref = element.Get("ref"),
                                    AttributeValue = element.Get("value"),
                                    Other = element.Attributes().GetRemaining(["id", "translate", "type", "value", "ref", "startRef"]),
                                };

                                parts.Add(annotation);
                                if (element.Name == ns + "mrk")
                                {
                                    var endAnnotation = new AnnotationEnd()
                                    {
                                        StartAnnotationReference = annotation,
                                    };
                                    annotation.EndAnnotationReference = endAnnotation;

                                    parts.AddRange(DeserializeLine(element, whiteSpaceHandling));
                                    parts.Add(endAnnotation);
                                }
                            }
                            else if (element.Name == ns + "em")
                            {
                                var endAnnotation = new AnnotationEnd();
                                var startRef = element.Get("startRef", Optionality.Required);
                                var matchingStartAnnotation = parts.OfType<AnnotationStart>().FirstOrDefault(x => x.Id == startRef);
                                if (matchingStartAnnotation is null)
                                {
                                    var tags = unit.Segments.SelectMany(x => x.Source.OfType<AnnotationStart>().Select(y => y)).ToList();
                                    tags.AddRange(unit.Segments.SelectMany(x => x.Target.OfType<AnnotationStart>().Select(y => y)));
                                    matchingStartAnnotation = tags.FirstOrDefault(x => x.Id == startRef);
                                }
                                endAnnotation.StartAnnotationReference = matchingStartAnnotation;
                                parts.Add(endAnnotation);
                            }
                            else if (element.Name == ns + "pc")
                            {
                                var startTag = new StartTag(true);
                                var endTag = new EndTag()
                                {
                                    StartTag = startTag,
                                };
                                startTag.EndTag = endTag;

                                SetCommonTagProperties(startTag, element);
                                SetTagValueFromDataRef(startTag, element.Get("dataRefStart"));
                                AddPossibleSubflows(startTag, element.Get("subFlowsStart"));
                                startTag.Display = element.Get("dispStart");
                                startTag.Equivalent = element.Get("equivStart");

                                SetTagValueFromDataRef(endTag, element.Get("dataRefEnd"));
                                AddPossibleSubflows(endTag, element.Get("subFlowsEnd"));
                                endTag.Display = element.Get("dispEnd");
                                endTag.Equivalent = element.Get("equivEnd");

                                parts.Add(startTag);
                                parts.AddRange(DeserializeLine(element, whiteSpaceHandling));
                                parts.Add(endTag);
                            }
                            else
                            {
                                InlineTag tag;
                                if (element.Name == ns + "sc")
                                {
                                    tag = new StartTag();
                                }
                                else if (element.Name == ns + "ec")
                                {
                                    tag = new EndTag();
                                    if (element.Get("startRef") is not null)
                                    {
                                        var startRef = element.Get("startRef");
                                        var matchingStartTag = parts.OfType<StartTag>().FirstOrDefault(x => x.Id == startRef);
                                        if (matchingStartTag is null)
                                        {
                                            var tags = unit.Segments.SelectMany(x => x.Source.OfType<StartTag>().Select(y => y)).ToList();
                                            tags.AddRange(unit.Segments.SelectMany(x => x.Target.OfType<StartTag>().Select(y => y)));
                                            matchingStartTag = tags.FirstOrDefault(x => x.Id == startRef);
                                        }

                                        if (matchingStartTag is not null)
                                        {
                                            matchingStartTag.EndTag = tag as EndTag;
                                            ((EndTag)tag).StartTag = matchingStartTag;
                                        }
                                    }
                                }
                                else
                                {
                                    tag = new InlineTag();
                                }

                                SetCommonTagProperties(tag, element);
                                tag.Display = element.Get("display");
                                tag.Equivalent = element.Get("equiv");
                                SetTagValueFromDataRef(tag, element.Get("dataRef"));
                                AddPossibleSubflows(tag, element.Get("subFlows"));

                                parts.Add(tag);
                            }
                        }

                        if (parts.Count > 0)
                        {
                            parts[0].Value = parts[0].Value.TrimStart();
                            parts[parts.Count - 1].Value = parts[parts.Count - 1].Value.TrimEnd();
                        }

                        return parts;
                    }

                    var segment = new Segment(contentCoder)
                    {
                        Id = node.Get("id"),
                        Source = DeserializeLine(sourceNode, sourceNode.GetWhiteSpaceHandling(whiteSpaceHandling)),
                        SourceWhiteSpaceHandling = sourceNode.GetWhiteSpaceHandling(whiteSpaceHandling),
                        CanResegment = node.GetBool("canResegment"),
                        State = node.GetState("state"),
                        SubState = node.Get("subState"),
                        Ignorable = node.Name == ns + "ignorable",
                        SourceAttributes = sourceNode.Attributes().ToList(),
                    };

                    if (targetNode != null)
                    {
                        segment.TargetWhiteSpaceHandling = targetNode.GetWhiteSpaceHandling(whiteSpaceHandling);
                        segment.Target = DeserializeLine(targetNode, segment.TargetWhiteSpaceHandling);
                        segment.Order = targetNode.GetInt("order");
                        segment.TargetAttributes = targetNode.Attributes().GetRemaining(["order"]);
                    }

                    return segment;
                }

                unit.Segments.AddRange(node.Elements().Where(x => x.Name == ns + "segment" || x.Name == ns + "ignorable").Select(DeserializeSegment));

                return unit;
            }

            Group DeserializeGroup(XElement node)
            {
                whiteSpaceHandling = node.GetWhiteSpaceHandling(whiteSpaceHandling);
                var group = new Group
                {
                    Id = node.Get("id", Optionality.Required),
                    Name = node.Get("name"),
                    CanResegment = node.GetBool("canResegment"),
                    Translate = node.GetBool("translate"),
                    SourceDirection = node.GetDirection("srcDir"),
                    TargetDirection = node.GetDirection("trgDir"),
                    Notes = DeserializeNotes(node.Element(ns + "notes")),
                    MetaData = DeserializeMetadata(node.Element(MetaNs + "metadata")),
                    Quality = DeserializeLocQuality(node),
                    Provenance = DeserializeProvenance(node),
                    FormatStyle = DeserializeFormatStyle(node),
                };
                group.Other.AddRange(node.Elements().GetRemaining([
                    ns + "notes", 
                    ns + "group",
                    ns + "unit", 
                    MetaNs + "metadata"]));
                group.Other.AddRange(node.Attributes().GetRemaining([.. CommonNodeLevelAttributes, "name"]));
                group.Children.AddRange(node.Elements(ns + "group").Select(DeserializeGroup));
                group.Children.AddRange(node.Elements(ns + "unit").Select(DeserializeUnit));
                return group;
            }

            foreach(var n in node.Elements())
            {
                if (n.Name == ns + "group")
                {
                    transformation.Children.Add(DeserializeGroup(n));
                }
                if (n.Name == ns + "unit")
                {
                    transformation.Children.Add(DeserializeUnit(n));
                }
            }

            // Fix unit references
            foreach (var (tag, ids) in unitReferences)
            {
                tag.UnitReferences = ids?.Select(x => transformation.GetUnits().FirstOrDefault(y => y.Id == x)!).Where(x => x is not null)?.ToList() ?? [];
            }

            return transformation;
        }

        if (version >= Xliff2Version.Xliff22)
        {
            DeserializeMetadata(xliffNode.Element(MetaNs + "metadata"), null, true);
            DeserializeNotes(xliffNode.Element(ns + "notes"), true);
        }

        var files = xliffNode.Elements(ns + "file").Select(DeserializeTransformation).ToList();
        Transformation transformation;
        if (files.Count() == 1)
        {
            transformation = files.First();
            transformation.MetaData.AddRange(globalMetadata);
            transformation.Notes.AddRange(globalNotes);
        }
        else
        {
            transformation = new Transformation(sourceLanguage, targetLanguage);
            transformation.Children.AddRange(files);
            transformation.MetaData = globalMetadata;
            transformation.Notes = globalNotes;
        }
        transformation.XliffOther.AddRange(xliffNode.Attributes().GetRemaining(["srcLang", "trgLang", "version", "xmlns"]));
        return transformation;
    }

    public static string Serialize(Transformation xliffTransformation, Xliff2Version version = Xliff2Version.Xliff22)
    {
        XNamespace ns = $"urn:oasis:names:tc:xliff:document:{version.Serialize()}";
        bool metaUsed = false;
        bool itsUsed = false;
        bool fsUsed = false;
        bool sizeRestrictionUsed = false;

        XElement? SerializeNotes(List<Note> notes, bool global = false)
        {
            if (notes.Count == 0) return null;
            var root = new XElement(ns + "notes");
            foreach (var note in notes)
            {
                var noteRoot = new XElement(ns + "note");
                noteRoot.Set("id", note.Id);
                noteRoot.SetInt("priority", note.Priority);
                noteRoot.Set("category", note.Global && !global ? Meta.Categories.Global + note.Category : note.Category);
                noteRoot.SetLanguageTarget("appliesTo", note.LanguageTarget);
                noteRoot.Value = note.Text;
                // TODO: Implement reference property handling
                noteRoot.Add(note.Other);
                root.Add(noteRoot);
            }
            return root;
        }

        void SerializeQuality(XElement element, Quality quality)
        {
            if (!quality.IsEmpty())
            {
                itsUsed = true;
            }
            element.SetDouble(ItsNs + "locQualityRatingScore", quality.Score);
            element.SetDouble(ItsNs + "locQualityRatingScoreThreshold", quality.ScoreThreshold);
            element.SetInt(ItsNs + "locQualityRatingVote", quality.Votes);
            element.SetInt(ItsNs + "locQualityRatingVoteThreshold", quality.VoteThreshold);
            element.Set(ItsNs + "locQualityRatingProfileRef", quality.ProfileReference);
        }

        void SerializeProvenance(XElement element, Provenance provenance) 
        { 
            if (!provenance.IsEmpty())
            {
                itsUsed = true;
            }

            element.Set(ItsNs + "person", provenance.Translation.Person);
            element.Set(ItsNs + "personRef", provenance.Translation.PersonReference);
            element.Set(ItsNs + "org", provenance.Translation.Organization);
            element.Set(ItsNs + "orgRef", provenance.Translation.OrganizationReference);
            element.Set(ItsNs + "tool", provenance.Translation.Tool);
            element.Set(ItsNs + "toolRef", provenance.Translation.ToolReference);

            element.Set(ItsNs + "revPerson", provenance.Review.Person);
            element.Set(ItsNs + "revPersonRef", provenance.Review.PersonReference);
            element.Set(ItsNs + "revOrg", provenance.Review.Organization);
            element.Set(ItsNs + "revOrgRef", provenance.Review.OrganizationReference);
            element.Set(ItsNs + "revTool", provenance.Review.Tool);
            element.Set(ItsNs + "revToolRef", provenance.Review.ToolReference);
        }

        void SerializeFormatStyle(XElement element, FormatStyle style)
        {
            if (!style.Tag.HasValue) return;
            fsUsed = true;
            element.Set(FormatStyleNs + "fs", style.Tag.Value.ToTag());
            element.Set(FormatStyleNs + "subFs", SubFsHelper.ToSubFsString(style.Attributes));
        }

        void SerializeSizeRestriction(XElement element, SizeRestrictions sizeRestrictions)
        {
            var serialized = SizeRestrictionHelper.Serialize(sizeRestrictions);
            if (serialized == null) return;
            sizeRestrictionUsed = true;
            element.Set(SizeRestrictionNs + "sizeRestriction", serialized);
        }

        XElement? SerializeMetadata(List<Metadata> metadata, bool global = false)
        {
            if (metadata.Count == 0) return null;
            var root = new XElement(MetaNs + "metadata");

            XElement FindCategory(XElement element, List<string> category)
            {
                if (category.Count == 0) return element;
                var child = element.Elements(MetaNs + "metaGroup").Where(x => x.Get("category") == category[0]).FirstOrDefault();

                if (child is null)
                {
                    var newCategory = new XElement(MetaNs + "metaGroup", new XAttribute("category", category[0]));
                    element.Add(newCategory);
                    child = newCategory;
                }

                return FindCategory(child, category.Skip(1).ToList());    
            }

            foreach (var meta in metadata)
            {
                if (meta.Global && !global)
                {
                    meta.Category.Insert(0, Meta.Categories.Global);
                }
                var metaRoot = new XElement(MetaNs + "meta");
                metaRoot.Set("type", meta.Type);
                metaRoot.Value = meta.Value;

                var categoryNode = FindCategory(root, meta.Category);
                categoryNode.Add(metaRoot);
                metaUsed = true;
            }

            return root;
        }

        IEnumerable<XNode> ReplaceInvalidXmlChars(string input)
        {
            var nodes = new List<XNode>();
            var textBuf = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                int cp = char.ConvertToUtf32(input, i);

                bool valid =
                       cp == 0x9 || cp == 0xA || cp == 0xD ||
                      (cp >= 0x20 && cp <= 0xD7FF) ||
                      (cp >= 0xE000 && cp <= 0xFFFD) ||
                      (cp >= 0x10000 && cp <= 0x10FFFF);

                if (valid)
                {
                    textBuf.Append(char.ConvertFromUtf32(cp));
                }
                else
                {
                    FlushText();
                    nodes.Add(new XElement(ns + "cp", new XAttribute("hex", cp.ToString("X4"))));
                }

                if (char.IsHighSurrogate(input[i])) i++;
            }

            FlushText();
            return nodes;

            void FlushText()
            {
                if (textBuf.Length == 0) return;
                nodes.Add(new XText(textBuf.ToString()));
                textBuf.Clear();
            }
        }

        var GetUniqueFileId = UniqueIdGenerator("f");
        XElement SerializeTransformation(Transformation transformation)
        {
            var GetUniqueGroupId = UniqueIdGenerator("g");
            var GetUniqueUnitId = UniqueIdGenerator("u");
            XElement SerializeUnit(Unit unit)
            {
                var originalData = new List<XElement>();
                var GetUniqueDataId = UniqueIdGenerator("d");
                var GetUniqueId = UniqueIdGenerator();
                XElement SerializeSegment(Segment segment)
                {
                    XElement SerializeParts(List<LineElement> parts, string elementName, List<LineElement>? idTagsToMatch = null)
                    {
                        void SetCommonInlineArguments(XElement element, InlineTag tag)
                        {
                            element.SetBool("canCopy", tag.CanCopy);
                            element.SetBool("canDelete", tag.CanDelete);
                            element.SetReorder("canReorder", tag.CanReorder);
                            element.Set("copyOf", tag.CopyOf);
                            element.SetBool("canOverlap", tag.CanOverlap);
                            element.SetDirection("dir", tag.Direction);
                            element.Set("subType", tag.SubType);
                            element.SetInlineType("type", tag.Type);
                            element.SetBool("isolated", tag.Isolated);
                            element.Add(tag.Other);
                            SerializeFormatStyle(element, tag.FormatStyle);
                        }

                        void SetCommonAnnotationArguments(XElement element, AnnotationStart annotationStart)
                        {
                            var id = GetUniqueId(annotationStart.Id ?? FindMatchingTagId(annotationStart));
                            annotationStart.Id = id;
                            element.Set("id", annotationStart.Id);
                            element.SetBool("translate", annotationStart.Translate);
                            element.Set("type", annotationStart.Type);
                            element.Set("ref", annotationStart.Ref);
                            element.Set("value", annotationStart.AttributeValue);
                            element.Add(annotationStart.Other);
                        }

                        void AddDataRefToElement(XElement element, InlineTag? tag, string property)
                        {
                            if (!string.IsNullOrEmpty(tag?.Value))
                            {
                                var existingDataValue = originalData.FirstOrDefault(x => x.Value == tag.Value);
                                if (existingDataValue != null)
                                {
                                    element.Set(property, existingDataValue.Get("id", Optionality.Required));
                                }
                                else
                                {
                                    var newDataElement = new XElement(ns + "data");
                                    var id = GetUniqueDataId(tag.DataId);
                                    newDataElement.Set("id", id);
                                    newDataElement.SetDirection("dir", tag.DataDirection);
                                    newDataElement.Add(ReplaceInvalidXmlChars(tag.Value));
                                    originalData.Add(newDataElement);
                                    element.Set(property, id);
                                }
                            }
                        }

                        void AddSubflowsToElement(XElement element, InlineTag? tag, string property)
                        {
                            if (tag != null && tag.UnitReferences.Count != 0)
                            {
                                List<Unit> references = [];
                                foreach(var unit in tag.UnitReferences)
                                {
                                    if (unit.Id is null) unit.Id = GetUniqueUnitId(unit.Id);
                                    references.Add(unit);
                                }
                                element.Set(property, string.Join(' ', references.Select(x => x.Id)));
                            }
                        }

                        Dictionary<string, int> passedTagInferences = new Dictionary<string, int>
                        {
                            { "mrk", 0 },
                            { "sm", 0 },
                            { "pc", 0 },
                            { "ph", 0 },
                        };

                        string? FindMatchingTagId(LineElement part)
                        {
                            T? FindTagGivenPassedReferences<T>(List<T> tags, string type)
                            {
                                if (!passedTagInferences.ContainsKey(type))
                                {
                                    passedTagInferences[type] = 0;
                                }
                                var numberOfPassedTags = passedTagInferences[type];
                                if (tags.Count() <= numberOfPassedTags) return default;
                                passedTagInferences[type] = numberOfPassedTags + 1;
                                return tags[numberOfPassedTags];
                            }

                            if (idTagsToMatch == null) return null;
                            if (part is StartTag wellFormedTag && wellFormedTag.WellFormed)
                            {
                                var tags = idTagsToMatch.OfType<StartTag>().Where(x => x.Id is not null && x.WellFormed).ToList();
                                return FindTagGivenPassedReferences(tags, "ph")?.Id;
                                
                            }
                            else if (part is StartTag startTag)
                            {
                                var tags = idTagsToMatch.OfType<StartTag>().Where(x => x.Id is not null && !x.WellFormed && x.Value == part.Value).ToList();
                                return FindTagGivenPassedReferences(tags, startTag.Value)?.Id;
                            }
                            else if (part is EndTag endTag)
                            {
                                if (endTag.StartTag is not null && endTag.StartTag.WellFormed) return null;
                                var tags = idTagsToMatch.OfType<EndTag>().Where(x => x.Id is not null && x.StartTag is not null && !x.StartTag.WellFormed && x.Value == part.Value).ToList();
                                return FindTagGivenPassedReferences(tags, endTag.Value)?.Id;
                            }
                            else if (part is InlineTag standaloneTag)
                            {
                                var tags = idTagsToMatch.OfType<InlineTag>().Where(x => x.Id is not null && x is not StartTag && x is not EndTag).ToList();
                                return FindTagGivenPassedReferences(tags, "pc")?.Id;
                            }
                            else if (part is AnnotationStart annotation && annotation.WellFormed)
                            {
                                var tags = idTagsToMatch.OfType<AnnotationStart>().Where(x => x.Id is not null && x.WellFormed).ToList();
                                return FindTagGivenPassedReferences(tags, "mrk")?.Id;
                            }
                            else if (part is AnnotationStart annotationStart)
                            {
                                var tags = idTagsToMatch.OfType<AnnotationStart>().Where(x => x.Id is not null && !x.WellFormed).ToList();
                                return FindTagGivenPassedReferences(tags, "sm")?.Id;
                            }
                            else
                            {
                                return null;
                            }
                        }

                        var root = new XElement(ns + elementName);
                        LineElement? skipUntil = null;
                        foreach (var part in parts)
                        {
                            if (skipUntil != null)
                            {
                                if (part == skipUntil)
                                {
                                    skipUntil = null;
                                }
                                continue;
                            }

                            if (part is StartTag wellFormedTag && wellFormedTag.WellFormed)
                            {
                                if (wellFormedTag.EndTag is null) throw new InvalidOperationException("Malformed data. Expected endtag for well formed start tag");
                                var children = GetChildrenOfStartTag(parts, wellFormedTag);
                                var element = SerializeParts(children, "pc");
                                var id = GetUniqueId(wellFormedTag.Id ?? FindMatchingTagId(wellFormedTag));
                                wellFormedTag.Id = id;
                                element.Set("id", wellFormedTag.Id);
                                AddDataRefToElement(element, wellFormedTag, "dataRefStart");
                                AddDataRefToElement(element, wellFormedTag.EndTag, "dataRefEnd");
                                AddSubflowsToElement(element, wellFormedTag, "subFlowsStart");
                                AddSubflowsToElement(element, wellFormedTag.EndTag, "subFlowsEnd");
                                SetCommonInlineArguments(element, wellFormedTag);
                                element.Set("equivStart", wellFormedTag.Equivalent);
                                element.Set("dispStart", wellFormedTag.Display);
                                element.Set("equivEnd", wellFormedTag.EndTag.Equivalent);
                                element.Set("dispEnd", wellFormedTag.EndTag.Display);
                                skipUntil = wellFormedTag.EndTag;
                                root.Add(element);
                            }
                            else if (part is StartTag startTag)
                            {
                                var startId = GetUniqueId(startTag.Id ?? FindMatchingTagId(startTag));
                                startTag.Id = startId;

                                var element = new XElement(ns + "sc");
                                element.Set("id", startId);
                                AddDataRefToElement(element, startTag, "dataRef");
                                AddSubflowsToElement(element, startTag, "subFlows");
                                element.Set("equiv", startTag.Equivalent);
                                element.Set("disp", startTag.Display);
                                SetCommonInlineArguments(element, startTag);
                                root.Add(element);
                            }
                            else if (part is EndTag endTag)
                            {
                                if (endTag.StartTag is not null && endTag.StartTag.WellFormed) continue;

                                var element = new XElement(ns + "ec");
                                element.Set("id", endTag.Id ?? FindMatchingTagId(endTag));
                                AddDataRefToElement(element, endTag, "dataRef");
                                AddSubflowsToElement(element, endTag, "subFlows");
                                SetCommonInlineArguments(element, endTag);
                                element.Set("equiv", endTag.Equivalent);
                                element.Set("disp", endTag.Display);
                                element.Set("startRef", endTag.StartTag?.Id);
                                root.Add(element);
                            }
                            else if (part is InlineTag standaloneTag)
                            {
                                var element = new XElement(ns + "ph");
                                var id = GetUniqueId(standaloneTag.Id ?? FindMatchingTagId(standaloneTag));
                                standaloneTag.Id = id;
                                element.Set("id", standaloneTag.Id);
                                AddDataRefToElement(element, standaloneTag, "dataRef");
                                AddSubflowsToElement(element, standaloneTag, "subFlows");
                                SetCommonInlineArguments(element, standaloneTag);
                                element.Set("equiv", standaloneTag.Equivalent);
                                element.Set("disp", standaloneTag.Display);
                                root.Add(element);
                            }
                            else if (part is AnnotationStart annotation && annotation.WellFormed)
                            {
                                var children = GetChildrenOfStartAnnotation(parts, annotation);
                                var element = SerializeParts(children, "mrk");
                                skipUntil = annotation.EndAnnotationReference;
                                SetCommonAnnotationArguments(element, annotation);
                                root.Add(element);
                            }
                            else if (part is AnnotationStart annotationStart)
                            {
                                var element = new XElement(ns + "sm");
                                SetCommonAnnotationArguments(element, annotationStart);
                                root.Add(element);
                            }
                            else if (part is AnnotationEnd annotationEnd)
                            {
                                var element = new XElement(ns + "em");
                                element.Set("startRef", annotationEnd.StartAnnotationReference?.Id);
                                root.Add(element);
                            }
                            else
                            {
                                root.Add(ReplaceInvalidXmlChars(part.Value));
                            }
                        }

                        return root;
                    }

                    var root = new XElement(ns + (segment.IsIgnorbale ? "ignorable" : "segment"));
                    root.Set("id", segment.Id);
                    root.SetBool("canResegment", segment.CanResegment);
                    root.SetState("state", segment.State);
                    root.Set("subState", segment.SubState);

                    var sourceNode = SerializeParts(segment.Source, "source");
                    sourceNode.Add(segment.SourceAttributes);
                    root.Add(sourceNode);

                    if (segment.Target.Count > 0)
                    {
                        var targetNode = SerializeParts(segment.Target, "target", segment.Source);
                        targetNode.SetInt("order", segment.Order);
                        targetNode.Add(segment.TargetAttributes);
                        root.Add(targetNode);
                    }
                    return root;
                }

                var root = new XElement(ns + "unit");
                var segmentElements = unit.Segments.Select(SerializeSegment).ToList();

                unit.Id = GetUniqueUnitId(unit.Id);
                root.Set("id", unit.Id);
                root.Set("name", unit.Name);
                root.SetBool("canResegment", unit.CanResegment);
                root.SetBool("translate", unit.Translate);
                root.SetDirection("srcDir", unit.SourceDirection);
                root.SetDirection("trgDir", unit.TargetDirection);
                
                root.Add(SerializeMetadata(unit.MetaData));
                root.Add(unit.Other);
                root.Add(SerializeNotes(unit.Notes));
                SerializeQuality(root, unit.Quality);
                SerializeProvenance(root, unit.Provenance);
                SerializeFormatStyle(root, unit.FormatStyle);
                SerializeSizeRestriction(root, unit.SizeRestrictions);
                if (originalData.Count != 0) root.Add(new XElement(ns + "originalData", originalData));
                root.Add(segmentElements);
                return root;
            }

            XElement SerializeGroup(Group group)
            {
                var root = new XElement(ns + "group");
                group.Id = GetUniqueGroupId(group.Id);
                root.Set("id", group.Id);
                root.Set("name", group.Name);
                root.SetBool("canResegment", group.CanResegment);
                root.SetBool("translate", group.Translate);
                root.SetDirection("srcDir", group.SourceDirection);
                root.SetDirection("trgDir", group.TargetDirection);
                root.Add(SerializeMetadata(group.MetaData));
                root.Add(group.Other);
                root.Add(SerializeNotes(group.Notes));
                root.Add(group.Children.OfType<Group>().Select(SerializeGroup));
                root.Add(group.Children.OfType<Unit>().Select(SerializeUnit));
                SerializeQuality(root, group.Quality);
                SerializeProvenance(root, group.Provenance);
                SerializeFormatStyle(root, group.FormatStyle);
                return root;
            }

            var root = new XElement(ns + "file");
            transformation.Id = GetUniqueFileId(transformation.Id);

            var notes = new List<Note>();
            var metadata = new List<Metadata>();
            notes = transformation.Notes.Where(x => !x.Global).ToList();
            metadata = transformation.MetaData.Where(x => !x.Global).ToList();

            void SetBlackbirdMetadata(string type, string? value)
            {
                var existing = metadata.FirstOrDefault(x => x.Category.Contains(Meta.Categories.Blackbird) && x.Type == type);

                if (existing is not null)
                {
                    if (value is null)
                    {
                        metadata.Remove(existing);
                    }
                    else
                    {
                        existing.Value = value;
                    }
                }
                else if (value is not null)
                {
                    metadata.Add(new Metadata(type, value) { Category = [Meta.Categories.Blackbird] });
                }
            }

            SetBlackbirdMetadata(Meta.Direction.Source + Meta.Types.ContentId,      transformation.SourceSystemReference.ContentId);
            SetBlackbirdMetadata(Meta.Direction.Source + Meta.Types.ContentName,    transformation.SourceSystemReference.ContentName);
            SetBlackbirdMetadata(Meta.Direction.Source + Meta.Types.AdminUrl,       transformation.SourceSystemReference.AdminUrl);
            SetBlackbirdMetadata(Meta.Direction.Source + Meta.Types.PublicUrl,      transformation.SourceSystemReference.PublicUrl);
            SetBlackbirdMetadata(Meta.Direction.Source + Meta.Types.SystemName,     transformation.SourceSystemReference.SystemName);
            SetBlackbirdMetadata(Meta.Direction.Source + Meta.Types.SystemRef,      transformation.SourceSystemReference.SystemRef);

            SetBlackbirdMetadata(Meta.Direction.Target + Meta.Types.ContentId,      transformation.TargetSystemReference.ContentId);
            SetBlackbirdMetadata(Meta.Direction.Target + Meta.Types.ContentName,    transformation.TargetSystemReference.ContentName);
            SetBlackbirdMetadata(Meta.Direction.Target + Meta.Types.AdminUrl,       transformation.TargetSystemReference.AdminUrl);
            SetBlackbirdMetadata(Meta.Direction.Target + Meta.Types.PublicUrl,      transformation.TargetSystemReference.PublicUrl);
            SetBlackbirdMetadata(Meta.Direction.Target + Meta.Types.SystemName,     transformation.TargetSystemReference.SystemName);
            SetBlackbirdMetadata(Meta.Direction.Target + Meta.Types.SystemRef,      transformation.TargetSystemReference.SystemRef);

            if (version < Xliff2Version.Xliff22)            
            {
                notes.AddRange(xliffTransformation.Notes);
                metadata.AddRange(xliffTransformation.MetaData);
            }

            root.Set("id", transformation.Id);
            root.SetBool("canResegment", transformation.CanResegment);
            root.SetBool("translate", transformation.Translate);
            root.Set("original", transformation.ExternalReference);
            root.SetDirection("srcDir", transformation.SourceDirection);
            root.SetDirection("trgDir", transformation.TargetDirection);

            if (transformation.Original is not null || transformation.OriginalReference is not null)
            {
                var skeleton = new XElement(ns + "skeleton", transformation.Original, transformation.SkeletonOther);
                skeleton.Set("href", transformation.OriginalReference);
                root.Add(skeleton);
            }

            root.Add(SerializeMetadata(metadata));
            root.Add(transformation.Other);
            root.Add(SerializeNotes(notes));
            SerializeQuality(root, transformation.Quality);
            SerializeProvenance(root, transformation.Provenance);
            SerializeFormatStyle(root, transformation.FormatStyle);

            foreach (var child in transformation.Children)
            {
                if (child is Group group)
                {
                    root.Add(SerializeGroup(group));
                }
                if (child is Unit unit)
                {
                    root.Add(SerializeUnit(unit));
                }
            }

            if (sizeRestrictionUsed && !root.Elements(SizeRestrictionNs + "profiles").Any())
            {
                var profilesNode = new XElement(SizeRestrictionNs + "profiles");
                profilesNode.Set("generalProfile", "xliff:codepoints");
                profilesNode.Set("storageProfile", "");
                root.AddFirst(profilesNode);
            }

            return root;
        }

        var root = new XElement(ns + "xliff");
        root.Set("srcLang", xliffTransformation.SourceLanguage);
        root.Set("trgLang", xliffTransformation.TargetLanguage);
        root.SetXliffVersion("version", version);

        root.Add(xliffTransformation.XliffOther);

        if (version >= Xliff2Version.Xliff22)
        {
            root.Add(SerializeNotes(xliffTransformation.Notes.Where(x => x.Global).ToList(), true));
            root.Add(SerializeMetadata(xliffTransformation.MetaData.Where(x => x.Global).ToList(), true));
        }

        if (!xliffTransformation.Children.OfType<Transformation>().Any())
        {
            root.Add(SerializeTransformation(xliffTransformation));
        }
        else
        {
            root.Add(xliffTransformation.Children.OfType<Transformation>().Select(SerializeTransformation).ToList());
        }

        if (metaUsed && root.Attribute(XNamespace.Xmlns + "mda") == null)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "mda", MetaNs));
        }

        if (itsUsed && root.Attribute(XNamespace.Xmlns + "its") == null)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "its", ItsNs));
        }

        if (fsUsed && root.Attribute(XNamespace.Xmlns + "fs") == null)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "fs", FormatStyleNs));
        }

        if (sizeRestrictionUsed && root.Attribute(XNamespace.Xmlns + "slr") == null)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "slr", SizeRestrictionNs));

        }

        var doc = new XDocument(root);
        return doc.ToString();
    }

    private static List<LineElement> GetChildrenOfStartTag(List<LineElement> parts, StartTag startTag)
    {
        var newList = new List<LineElement>();
        var collect = false;
        foreach (var part in parts)
        {
            if (part == startTag.EndTag)
            {
                break;
            }

            if (collect)
            {
                newList.Add(part);
            }

            if (part == startTag)
            {
                collect = true;
            }
        }
        return newList;
    }

    private static List<LineElement> GetChildrenOfStartAnnotation(List<LineElement> parts, AnnotationStart startAnnotation)
    {
        var newList = new List<LineElement>();
        var collect = false;
        foreach (var part in parts)
        {
            if (part == startAnnotation.EndAnnotationReference)
            {
                break;
            }

            if (collect)
            {
                newList.Add(part);
            }

            if (part == startAnnotation)
            {
                collect = true;
            }
        }
        return newList;
    }

    private static Func<string?, string> UniqueIdGenerator(string prefix = "")
    {
        List<string> ids = [];
        string GetUniqueId(string? incomingId)
        {
            if (!string.IsNullOrEmpty(incomingId))
            {
                ids.Add(incomingId);
                return incomingId;
            }
            var candidate = 1;
            var stringCandidate = $"{prefix}{candidate}";
            while (ids.Contains(stringCandidate))
            {
                candidate++;
                stringCandidate = $"{prefix}{candidate}";
            }
            ids.Add(stringCandidate);
            return stringCandidate;
        }
        return GetUniqueId;
    }

    private static XElement? GetRootNode(string content)
    {
        try
        {
            var doc = XDocument.Parse(content);
            return doc.Root;
        }
        catch (Exception)
        {
            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

            if (content.StartsWith(_byteOrderMarkUtf8))
            {
                content = content.Remove(0, _byteOrderMarkUtf8.Length);
            }

            var doc = XDocument.Parse(content);
            return doc.Root;
        }
    }

    public static bool IsXliff2(string content)
    {
        if (TryGetXliffVersion(content, out var version))
        {
            return version.StartsWith("2.");
        }

        return false;
    }

    public static bool TryGetXliffVersion(string content, out string version)
    {
        version = string.Empty;
        try
        {
            var xliffNode = GetRootNode(content);
            if (xliffNode == null)
                return false;

            if (xliffNode.Name.LocalName != "xliff")
                return false;

            var v = xliffNode.Get("version");
            if (string.IsNullOrEmpty(v))
                return false;

            version = v;
            return true;
        }
        catch (Exception)
        {
            version = string.Empty;
            return false;
        }
    }
}