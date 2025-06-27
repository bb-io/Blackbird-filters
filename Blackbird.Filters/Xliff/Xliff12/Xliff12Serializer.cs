using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;
using Blackbird.Filters.Transformations.Tags;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Blackbird.Filters.Xliff.Xliff2;

namespace Blackbird.Filters.Xliff.Xliff12;

public static class Xliff12Serializer
{
    private static readonly XNamespace BlackbirdNs = "http://blackbird.io/";
    private static readonly XNamespace XliffNs = "urn:oasis:names:tc:xliff:document:1.2";

    public static string Serialize(Transformation transformation)
    {
        var root = new XElement(XliffNs + "xliff",
            new XAttribute("version", "1.2"));

        SerializeTransformation(transformation, root);

        var doc = new XDocument(root);
        return doc.ToString();
    }

    private static void SerializeTransformation(Transformation transformation, XElement root)
    {
        var fileId = UniqueIdGenerator("f");

        XElement SerializeFile(Transformation file)
        {
            file.Id = fileId(file.Id);
            var fileElement = new XElement(XliffNs + "file",
                new XAttribute("id", file.Id),
                new XAttribute("original", file.ExternalReference ?? string.Empty),
                new XAttribute("source-language", file.SourceLanguage ?? "en"),
                new XAttribute("datatype", "plaintext")); // Default datatype

            if (file.TargetLanguage != null)
            {
                fileElement.SetAttributeValue("target-language", file.TargetLanguage);
            }

            // Add header
            var header = new XElement(XliffNs + "header");
            if (file.Notes.Count > 0)
            {
                foreach (var note in file.Notes)
                {
                    var noteElement = new XElement(XliffNs + "note", note.Text);
                    if (note.Priority.HasValue)
                        noteElement.SetAttributeValue("priority", note.Priority.Value);
                    header.Add(noteElement);
                }
            }

            // Add skeleton if present
            if (!string.IsNullOrEmpty(file.Original) || !string.IsNullOrEmpty(file.OriginalReference))
            {
                var skeleton = new XElement(XliffNs + "skl");
                
                if (!string.IsNullOrEmpty(file.OriginalReference))
                {
                    var externalFile = new XElement(XliffNs + "external-file",
                        new XAttribute("href", file.OriginalReference));
                    skeleton.Add(externalFile);
                }
                else if (!string.IsNullOrEmpty(file.Original))
                {
                    var internalFile = new XElement(XliffNs + "internal-file", 
                        new XCData(file.Original));
                    skeleton.Add(internalFile);
                }

                header.Add(skeleton);
            }

            fileElement.Add(header);

            // Create body and add content
            var body = new XElement(XliffNs + "body");
            SerializeGroupsAndUnits(file, body);
            fileElement.Add(body);

            return fileElement;
        }

        if (!transformation.Children.OfType<Transformation>().Any())
        {
            root.Add(SerializeFile(transformation));
        }
        else
        {
            foreach (var file in transformation.Children.OfType<Transformation>())
            {
                root.Add(SerializeFile(file));
            }
        }
    }

    private static void SerializeGroupsAndUnits(Node node, XElement parent)
    {
        var groupId = UniqueIdGenerator("g");
        var unitId = UniqueIdGenerator("u");
        var segmentId = UniqueIdGenerator("s");
        
        void SerializeGroup(Group group, XElement parentElement)
        {
            group.Id = groupId(group.Id);
            var groupElement = new XElement(XliffNs + "group",
                new XAttribute("id", group.Id));
            
            if (group.Name != null)
                groupElement.SetAttributeValue("resname", group.Name);
                
            if (group.Translate.HasValue && !group.Translate.Value)
                groupElement.SetAttributeValue("translate", group.Translate.Value ? "yes" : "no");
                
            // Add notes if any
            foreach (var note in group.Notes)
            {
                var noteElement = new XElement(XliffNs + "note", note.Text);
                if (!string.IsNullOrEmpty(note.Id))
                    noteElement.SetAttributeValue("id", note.Id);
                groupElement.Add(noteElement);
            }
            
            // Process child groups and units
            foreach (var child in group.Children)
            {
                if (child is Group childGroup)
                    SerializeGroup(childGroup, groupElement);
                else if (child is Unit unit)
                    SerializeUnit(unit, groupElement);
            }
            
            parentElement.Add(groupElement);
        }
        
        void SerializeUnit(Unit unit, XElement parentElement)
        {
            // Skip empty units
            if (!unit.Segments.Any())
                return;
                
            unit.Id = unitId(unit.Id);
            
            // In XLIFF 1.2, segments are gathered under trans-units
            // We'll create one trans-unit per unit
            var transUnit = new XElement(XliffNs + "trans-unit",
                new XAttribute("id", unit.Id));
            
            if (unit.Name != null)
                transUnit.SetAttributeValue("resname", unit.Name);
                
            if (unit.Translate.HasValue && !unit.Translate.Value)
                transUnit.SetAttributeValue("translate", unit.Translate.Value ? "yes" : "no");
            
            // Handle simple case first - one segment
            if (unit.Segments.Count == 1)
            {
                var segment = unit.Segments[0];
                transUnit.Add(SerializeTextParts(segment.Source, "source"));
                
                if (segment.Target.Any())
                    transUnit.Add(SerializeTextParts(segment.Target, "target"));
                    
                // Add state if present
                if (segment.State.HasValue)
                {
                    string stateStr = segment.State.Value switch
                    {
                        SegmentState.Initial => "new",
                        SegmentState.Translated => "translated",
                        SegmentState.Reviewed => "reviewed",
                        SegmentState.Final => "final",
                        _ => "new"
                    };

                    if (segment.State == SegmentState.Final)
                    {
                        transUnit.SetAttributeValue("approved", "yes");
                    }
                }
            }
            // Multiple segments - use seg-source and mrk elements
            else if (unit.Segments.Count > 1)
            {
                // Create combined source
                var sourceContent = new StringBuilder();
                foreach (var segment in unit.Segments)
                {
                    sourceContent.Append(string.Join("", segment.Source.Select(p => p.Value)));
                }
                
                transUnit.Add(new XElement(XliffNs + "source", sourceContent.ToString()));
                
                // Create seg-source with mrk elements
                var segSource = new XElement(XliffNs + "seg-source");
                int segIndex = 1;
                foreach (var segment in unit.Segments)
                {
                    var mrkElement = SerializeTextParts(segment.Source, "mrk");
                    mrkElement.SetAttributeValue("mtype", "seg");
                    mrkElement.SetAttributeValue("mid", segIndex.ToString());
                    segSource.Add(mrkElement);
                    segIndex++;
                }
                transUnit.Add(segSource);
                
                // Add target with segmentation if targets exist
                if (unit.Segments.Any(s => s.Target.Any()))
                {
                    var target = new XElement(XliffNs + "target");
                    segIndex = 1;
                    foreach (var segment in unit.Segments)
                    {
                        if (segment.Target.Any())
                        {
                            var mrkElement = SerializeTextParts(segment.Target, "mrk");
                            mrkElement.SetAttributeValue("mtype", "seg");
                            mrkElement.SetAttributeValue("mid", segIndex.ToString());
                            target.Add(mrkElement);
                        }
                        segIndex++;
                    }
                    transUnit.Add(target);
                }
            }
            
            // Add notes if any
            foreach (var note in unit.Notes)
            {
                var noteElement = new XElement(XliffNs + "note", note.Text);
                transUnit.Add(noteElement);
            }
            
            parentElement.Add(transUnit);
        }
        
        // Process the node children
        if (node is Group group)
        {
            SerializeGroup(group, parent);
        }
        else if (node is Unit unit)
        {
            SerializeUnit(unit, parent);
        }
        else if (node is Transformation transformation)
        {
            foreach (var child in transformation.Children)
            {
                if (child is Group childGroup)
                    SerializeGroup(childGroup, parent);
                else if (child is Unit childUnit)
                    SerializeUnit(childUnit, parent);
                else if (child is Transformation childTransformation)
                    SerializeGroupsAndUnits(childTransformation, parent);
            }
        }
    }
    
    private static XElement SerializeTextParts(List<TextPart> parts, string elementName)
    {
        var element = new XElement(XliffNs + elementName);
        
        foreach (var part in parts)
        {
            if (part is InlineTag tag)
            {
                if (part is StartTag startTag && startTag.WellFormed && startTag.EndTag != null)
                {
                    // Create a g element for paired tags
                    var gElement = new XElement(XliffNs + "g");
                    if (!string.IsNullOrEmpty(startTag.Id))
                        gElement.SetAttributeValue("id", startTag.Id);
                        
                    // Find content between these tags
                    int startIndex = parts.IndexOf(startTag);
                    int endIndex = parts.IndexOf(startTag.EndTag);
                    
                    if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
                    {
                        for (int i = startIndex + 1; i < endIndex; i++)
                        {
                            if (parts[i] is TextPart textPart)
                                gElement.Add(textPart.Value);
                        }
                    }
                    
                    element.Add(gElement);
                }
                else if (part is StartTag st)
                {
                    // Create a bpt element for unpaired start tag
                    var bptElement = new XElement(XliffNs + "bpt");
                    if (!string.IsNullOrEmpty(st.Id))
                        bptElement.SetAttributeValue("id", st.Id);
                    
                    if (!string.IsNullOrEmpty(st.Value))
                        bptElement.Add(st.Value);
                        
                    element.Add(bptElement);
                }
                else if (part is EndTag et)
                {
                    // Create an ept element for unpaired end tag
                    var eptElement = new XElement(XliffNs + "ept");
                    if (!string.IsNullOrEmpty(et.Id))
                        eptElement.SetAttributeValue("id", et.Id);
                        
                    if (et.StartTag != null && !string.IsNullOrEmpty(et.StartTag.Id))
                        eptElement.SetAttributeValue("rid", et.StartTag.Id);
                        
                    if (!string.IsNullOrEmpty(et.Value))
                        eptElement.Add(et.Value);
                        
                    element.Add(eptElement);
                }
                else
                {
                    // Create a ph element for standalone tags
                    var phElement = new XElement(XliffNs + "ph");
                    if (!string.IsNullOrEmpty(tag.Id))
                        phElement.SetAttributeValue("id", tag.Id);
                        
                    if (!string.IsNullOrEmpty(tag.Value))
                        phElement.Add(tag.Value);
                        
                    element.Add(phElement);
                }
            }
            else if (part is AnnotationStart anno)
            {
                // Handle annotations with mrk
                if (anno.WellFormed && anno.EndAnnotationReference != null)
                {
                    var mrkElement = new XElement(XliffNs + "mrk");
                    
                    if (!string.IsNullOrEmpty(anno.Id))
                        mrkElement.SetAttributeValue("mid", anno.Id);
                        
                    // Use the annotation type or default to "x-annotation"
                    mrkElement.SetAttributeValue("mtype", !string.IsNullOrEmpty(anno.Type) ? anno.Type : "x-annotation");
                    
                    // Find content between these annotations
                    int startIndex = parts.IndexOf(anno);
                    int endIndex = parts.IndexOf(anno.EndAnnotationReference);
                    
                    if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
                    {
                        for (int i = startIndex + 1; i < endIndex; i++)
                        {
                            if (parts[i] is TextPart textPart)
                                mrkElement.Add(textPart.Value);
                        }
                    }
                    
                    element.Add(mrkElement);
                }
            }
            else if (!(part is AnnotationEnd))  // Skip standalone end annotations
            {
                // Simple text
                element.Add(part.Value);
            }
        }
        
        return element;
    }
    
    public static Transformation Deserialize(string fileContent)
    {
        var xliffNode = GetRootNode(fileContent);
        
        if (xliffNode == null)
        {
            throw new Exception("No root node found in XLIFF content.");
        }
        
        var sourceLanguage = xliffNode.Elements(XliffNs + "file").FirstOrDefault()?.Attribute("source-language")?.Value;
        var targetLanguage = xliffNode.Elements(XliffNs + "file").FirstOrDefault()?.Attribute("target-language")?.Value;
        
        var transformation = new Transformation(sourceLanguage, targetLanguage);
        
        foreach (var fileElement in xliffNode.Elements(XliffNs + "file"))
        {
            var fileTransformation = new Transformation(
                fileElement.Attribute("source-language")?.Value ?? sourceLanguage,
                fileElement.Attribute("target-language")?.Value ?? targetLanguage)
            {
                Id = fileElement.Attribute("id")?.Value,
                ExternalReference = fileElement.Attribute("original")?.Value
            };
            
            // Process header
            var header = fileElement.Element(XliffNs + "header");
            if (header != null)
            {
                var skeleton = header.Element(XliffNs + "skl");
                if (skeleton != null)
                {
                    var internalFile = skeleton.Element(XliffNs + "internal-file");
                    if (internalFile != null)
                    {
                        fileTransformation.Original = internalFile.Value;
                    }
                    
                    var externalFile = skeleton.Element(XliffNs + "external-file");
                    if (externalFile != null)
                    {
                        fileTransformation.OriginalReference = externalFile.Attribute("href")?.Value;
                    }
                }
                
                // Process notes
                foreach (var note in header.Elements(XliffNs + "note"))
                {
                    fileTransformation.Notes.Add(new Note(note.Value)
                    {
                        Id = note.Attribute("id")?.Value,
                        Priority = int.TryParse(note.Attribute("priority")?.Value, out var priority) ? priority : null
                    });
                }
            }
            
            // Process body content
            var body = fileElement.Element(XliffNs + "body");
            if (body != null)
            {
                ProcessBodyContent(body, fileTransformation);
            }
            
            transformation.Children.Add(fileTransformation);
        }
        
        return transformation;
    }
    
    private static void ProcessBodyContent(XElement body, Node parent)
    {
        foreach (var element in body.Elements())
        {
            if (element.Name == XliffNs + "group")
            {
                var group = new Group
                {
                    Id = element.Attribute("id")?.Value,
                    Name = element.Attribute("resname")?.Value,
                    Translate = element.Attribute("translate") == null || element.Attribute("translate")?.Value == "yes"
                };
                
                // Process notes
                foreach (var note in element.Elements(XliffNs + "note"))
                {
                    group.Notes.Add(new Note(note.Value)
                    {
                        Id = note.Attribute("id")?.Value
                    });
                }
                
                // Process children
                ProcessBodyContent(element, group);
                
                // Add group to parent
                if (parent is Group parentGroup)
                    parentGroup.Children.Add(group);
                else if (parent is Transformation transformation)
                    transformation.Children.Add(group);
            }
            else if (element.Name == XliffNs + "trans-unit")
            {
                var unit = new Unit
                {
                    Id = element.Attribute("id")?.Value,
                    Name = element.Attribute("resname")?.Value,
                    Translate = element.Attribute("translate") == null || element.Attribute("translate")?.Value == "yes"
                };
                
                var source = element.Element(XliffNs + "source");
                var target = element.Element(XliffNs + "target");
                var segSource = element.Element(XliffNs + "seg-source");
                var codeType = element.GetCodeType(BlackbirdNs + "tagHandling");
                
                // Handle segmented content
                if (segSource != null)
                {
                    // Process segmented source
                    foreach (var mrkElement in segSource.Elements(XliffNs + "mrk").Where(m => m.Attribute("mtype")?.Value == "seg"))
                    {
                        var segment = new Segment
                        {
                            Id = mrkElement.Attribute("mid")?.Value,
                            Source = ExtractTextParts(mrkElement),
                            CodeType = codeType
                        };
                        
                        // Find matching target segment if any
                        if (target != null)
                        {
                            var midValue = mrkElement.Attribute("mid")?.Value;
                            var matchingTargetMrk = target.Elements(XliffNs + "mrk")
                                .FirstOrDefault(m => m.Attribute("mtype")?.Value == "seg" && 
                                                    m.Attribute("mid")?.Value == midValue);
                            
                            if (matchingTargetMrk != null)
                            {
                                segment.Target = ExtractTextParts(matchingTargetMrk);
                            }
                        }
                        
                        unit.Segments.Add(segment);
                    }
                }
                else
                {
                    // Handle non-segmented content
                    var segment = new Segment
                    {
                        Source = source != null ? ExtractTextParts(source) : new List<TextPart>(),
                        Target = target != null ? ExtractTextParts(target) : new List<TextPart>(),
                        CodeType = codeType
                    };
                    
                    // Set state if present
                    if (element.Attribute("approved")?.Value == "yes")
                        segment.State = SegmentState.Final;
                    else if (target != null)
                        segment.State = SegmentState.Translated;
                    
                    unit.Segments.Add(segment);
                }
                
                // Process notes
                foreach (var note in element.Elements(XliffNs + "note"))
                {
                    unit.Notes.Add(new Note(note.Value)
                    {
                        Id = note.Attribute("id")?.Value
                    });
                }
                
                // Add unit to parent
                if (parent is Group parentGroup)
                    parentGroup.Children.Add(unit);
                else if (parent is Transformation transformation)
                    transformation.Children.Add(unit);
            }
        }
    }
    
    private static List<TextPart> ExtractTextParts(XElement element)
    {
        var parts = new List<TextPart>();
        var idGenerator = UniqueIdGenerator();
        
        foreach (var node in element.Nodes())
        {
            if (node is XText textNode)
            {
                parts.Add(new TextPart { Value = textNode.Value });
            }
            else if (node is XElement childElement)
            {
                if (childElement.Name == XliffNs + "g")
                {
                    // Handle paired tags (g element)
                    var startTag = new StartTag(true) { Id = childElement.Attribute("id")?.Value ?? idGenerator(null) };
                    var endTag = new EndTag { StartTag = startTag };
                    startTag.EndTag = endTag;
                    
                    parts.Add(startTag);
                    parts.AddRange(ExtractTextParts(childElement));
                    parts.Add(endTag);
                }
                else if (childElement.Name == XliffNs + "bpt")
                {
                    // Unpaired start tag
                    var startTag = new StartTag { 
                        Id = childElement.Attribute("id")?.Value ?? idGenerator(null),
                        Value = childElement.Value
                    };
                    parts.Add(startTag);
                }
                else if (childElement.Name == XliffNs + "ept")
                {
                    // Unpaired end tag
                    var endTag = new EndTag { 
                        Id = childElement.Attribute("id")?.Value,
                        Value = childElement.Value
                    };
                    
                    // Try to link with matching start tag
                    var ridValue = childElement.Attribute("rid")?.Value;
                    if (!string.IsNullOrEmpty(ridValue))
                    {
                        var matchingStartTag = parts
                            .OfType<StartTag>()
                            .FirstOrDefault(t => t.Id == ridValue);
                            
                        if (matchingStartTag != null)
                        {
                            endTag.StartTag = matchingStartTag;
                            matchingStartTag.EndTag = endTag;
                        }
                    }
                    
                    parts.Add(endTag);
                }
                else if (childElement.Name == XliffNs + "ph")
                {
                    // Placeholder tag
                    parts.Add(new InlineTag 
                    { 
                        Id = childElement.Attribute("id")?.Value ?? idGenerator(null),
                        Value = childElement.Value
                    });
                }
                else if (childElement.Name == XliffNs + "mrk")
                {
                    // Handle internal mrk elements (annotations)
                    var mtype = childElement.Attribute("mtype")?.Value;
                    if (mtype != "seg") // Skip segmentation marks
                    {
                        var annotation = new AnnotationStart(true)
                        {
                            Id = childElement.Attribute("mid")?.Value ?? idGenerator(null),
                            Type = mtype
                        };
                        
                        var endAnnotation = new AnnotationEnd { StartAnnotationReference = annotation };
                        annotation.EndAnnotationReference = endAnnotation;
                        
                        parts.Add(annotation);
                        parts.AddRange(ExtractTextParts(childElement));
                        parts.Add(endAnnotation);
                    }
                }
                else
                {
                    // For other elements, extract any text content
                    if (!string.IsNullOrEmpty(childElement.Value))
                    {
                        parts.Add(new TextPart { Value = childElement.Value });
                    }
                }
            }
        }
        
        return parts;
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

    public static bool IsXliff12(string content)
    {
        try
        {
            var xliffNode = GetRootNode(content);
            if (xliffNode == null)
            {
                return false;
            }

            // Check if the root is an XLIFF element with correct namespace
            if (xliffNode.Name.Namespace != XliffNs || xliffNode.Name.LocalName != "xliff")
            {
                return false;
            }

            // Check version attribute
            var version = xliffNode.Attribute("version")?.Value;
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            return version.StartsWith("1."); // Accept any 1.x version as XLIFF 1.x
        }
        catch (Exception)
        {
            return false;
        }
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
}