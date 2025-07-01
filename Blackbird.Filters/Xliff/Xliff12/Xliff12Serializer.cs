using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;
using Blackbird.Filters.Transformations.Tags;
using System.Text;
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
        var xmlString = doc.ToString();

        return CompactSourceElements(xmlString);
    }

    private static void SerializeTransformation(Transformation transformation, XElement root)
    {
        var fileId = UniqueIdGenerator("f");
        XElement SerializeFile(Transformation file)
        {
            file.Id = fileId(file.Id);
            var fileElement = new XElement(XliffNs + "file",
                new XAttribute("id", file.Id),
                new XAttribute("source-language", file.SourceLanguage ?? "en"));

            if (file.TargetLanguage != null)
            {
                fileElement.SetAttributeValue("target-language", file.TargetLanguage);
            }

            if (!string.IsNullOrEmpty(file.ExternalReference))
            {
                fileElement.SetAttributeValue("original", file.ExternalReference);
            }

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

            foreach (var otherElements in file.Other)
            {
                if (otherElements is XElement otherElement)
                {
                    header.Add(otherElement);
                }
                else if (otherElements is XAttribute attribute)
                {
                    fileElement.SetAttributeValue(attribute.Name, attribute.Value);
                }
            }

            if (!string.IsNullOrEmpty(transformation.Original) || !string.IsNullOrEmpty(transformation.OriginalReference))
            {
                var skeleton = new XElement(XliffNs + "skl");

                if (!string.IsNullOrEmpty(transformation.OriginalReference))
                {
                    var externalFile = new XElement(XliffNs + "external-file",
                        new XAttribute("href", transformation.OriginalReference));
                    skeleton.Add(externalFile);
                }
                else if (!string.IsNullOrEmpty(transformation.Original))
                {
                    var internalFile = new XElement(XliffNs + "internal-file", transformation.Original, transformation.SkeletonOther);
                    skeleton.Add(internalFile);
                }

                header.Add(skeleton);
            }

            fileElement.Add(header);

            var body = new XElement(XliffNs + "body");
            SerializeGroupsAndUnits(file, body);
            fileElement.Add(body);

            return fileElement;
        }

        try
        {
            root.Add(transformation.XliffOther);
        }
        catch (InvalidOperationException e) when (e.Message.Contains("Duplicate attribute."))
        { }

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

        void SerializeGroup(Group group, XElement parentElement)
        {
            group.Id = groupId(group.Id);
            var groupElement = new XElement(XliffNs + "group", new XAttribute("id", group.Id));

            if (group.Name != null)
                groupElement.SetAttributeValue("resname", group.Name);

            foreach (var note in group.Notes)
            {
                var noteElement = new XElement(XliffNs + "note", note.Text);
                if (!string.IsNullOrEmpty(note.Id))
                    noteElement.SetAttributeValue("id", note.Id);
                groupElement.Add(noteElement);
            }

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
            if (!unit.Segments.Any())
                return;

            unit.Id = unitId(unit.Id);
            var transUnit = new XElement(XliffNs + "trans-unit",
                new XAttribute("id", unit.Id));

            if (unit.Name != null)
                transUnit.SetAttributeValue("resname", unit.Name);

            if (unit.Segments.Count == 1)
            {
                var segment = unit.Segments[0];
                transUnit.Add(SerializeTextParts(segment.Source, "source", segment.SourceAttributes, segment.SourceWhiteSpaceHandling));

                if (segment.Target.Any())
                {
                    var targetElement = SerializeTextParts(segment.Target, "target", segment.TargetAttributes, segment.TargetWhiteSpaceHandling);
                    if (segment.CanResegment.HasValue)
                    {
                        targetElement.SetAttributeValue(BlackbirdNs + "canResegment", segment.CanResegment.Value.ToString().ToLower());
                    }

                    transUnit.Add(targetElement);
                }

                if (segment.State is SegmentState.Final)
                {
                    transUnit.SetAttributeValue("approved", "yes");
                }

                if (!string.IsNullOrEmpty(segment.SubState))
                {
                    transUnit.SetAttributeValue("phase-name", segment.SubState);
                }

                if (segment.Ignorable.HasValue)
                {
                    transUnit.SetAttributeValue("translate", segment.Ignorable.Value ? "no" : "yes");
                }
            }
            else if (unit.Segments.Count > 1)
            {
                var sourceContent = new StringBuilder();
                foreach (var segment in unit.Segments)
                {
                    sourceContent.Append(string.Join("", segment.Source.Select(p => p.Value)));
                }

                transUnit.Add(new XElement(XliffNs + "source", sourceContent.ToString()));
                var segSource = new XElement(XliffNs + "seg-source");
                int segIndex = 1;
                foreach (var segment in unit.Segments)
                {
                    var mrkElement = SerializeTextParts(segment.Source, "mrk", null, segment.SourceWhiteSpaceHandling);
                    mrkElement.SetAttributeValue("mtype", "seg");
                    mrkElement.SetAttributeValue("mid", segIndex.ToString());
                    segSource.Add(mrkElement);
                    segIndex++;
                }

                transUnit.Add(segSource);
                if (unit.Segments.Any(s => s.Target.Any()))
                {
                    var target = new XElement(XliffNs + "target");
                    if (unit.Segments.FirstOrDefault()?.TargetAttributes?.Any() == true)
                    {
                        foreach (var attr in unit.Segments.First().TargetAttributes)
                        {
                            if (attr.Name != BlackbirdNs + "customState" && attr.Name != BlackbirdNs + "canResegment")
                            {
                                target.SetAttributeValue(attr.Name, attr.Value);
                            }
                        }
                    }

                    segIndex = 1;
                    foreach (var segment in unit.Segments)
                    {
                        if (segment.Target.Any())
                        {
                            var mrkElement = SerializeTextParts(segment.Target, "mrk", segment.TargetAttributes, segment.TargetWhiteSpaceHandling);
                            mrkElement.SetAttributeValue("mtype", "seg");
                            mrkElement.SetAttributeValue("mid", segIndex.ToString());

                            if (segment.State is SegmentState.Final)
                            {
                                if (unit.Segments.All(s => s.State is SegmentState.Final))
                                {
                                    transUnit.SetAttributeValue("approved", "yes");
                                }
                            }

                            if (segment.CanResegment.HasValue)
                            {
                                mrkElement.SetAttributeValue(BlackbirdNs + "canResegment", segment.CanResegment.Value.ToString().ToLower());
                            }

                            target.Add(mrkElement);
                        }
                        segIndex++;
                    }

                    transUnit.Add(target);
                }
            }

            transUnit.SetCodeType(BlackbirdNs + "tagHandling", unit.Segments.FirstOrDefault()?.CodeType);
            foreach (var note in unit.Notes)
            {
                var noteElement = new XElement(XliffNs + "note", note.Text);
                transUnit.Add(noteElement);
            }

            parentElement.Add(transUnit);
        }

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

    private static XElement SerializeTextParts(List<TextPart> parts, string elementName, IEnumerable<XAttribute>? attributes = null, WhiteSpaceHandling whiteSpaceHandling = WhiteSpaceHandling.Default)
    {
        var element = new XElement(XliffNs + elementName);
        if (whiteSpaceHandling == WhiteSpaceHandling.Preserve)
        {
            element.SetAttributeValue(XNamespace.Xml + "space", "preserve");
        }

        if (attributes != null)
        {
            foreach (var attr in attributes)
            {
                if (attr.Name != BlackbirdNs + "customState" && attr.Name != BlackbirdNs + "canResegment")
                {
                    element.SetAttributeValue(attr.Name, attr.Value);
                }
            }

            var customStateAttr = attributes.FirstOrDefault(a => a.Name == BlackbirdNs + "customState");
            if (customStateAttr != null && (elementName == "target" || elementName == "mrk"))
            {
                element.SetAttributeValue("state", customStateAttr.Value);
            }
        }

        var processedParts = new HashSet<TextPart>();
        foreach (var part in parts)
        {
            if (processedParts.Contains(part))
            {
                continue;
            }

            if (part is InlineTag tag)
            {
                if (part is StartTag startTag && startTag.WellFormed && startTag.EndTag != null)
                {
                    var gElement = new XElement(XliffNs + "g");
                    if (!string.IsNullOrEmpty(startTag.Id))
                        gElement.SetAttributeValue("id", startTag.Id);

                    foreach (var attr in startTag.Other.OfType<XAttribute>())
                    {
                        gElement.SetAttributeValue(attr.Name, attr.Value);
                    }

                    int startIndex = parts.IndexOf(startTag);
                    int endIndex = parts.IndexOf(startTag.EndTag);

                    if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
                    {
                        for (int i = startIndex + 1; i < endIndex; i++)
                        {
                            if (parts[i] is TextPart textPart && !processedParts.Contains(textPart))
                            {
                                gElement.Add(textPart.Value);
                                processedParts.Add(textPart);
                            }
                        }
                    }

                    element.Add(gElement);
                    processedParts.Add(startTag);
                    processedParts.Add(startTag.EndTag);
                }
                else if (part is StartTag st)
                {
                    var bptElement = new XElement(XliffNs + "bpt");
                    if (!string.IsNullOrEmpty(st.Id))
                        bptElement.SetAttributeValue("id", st.Id);

                    foreach (var attr in st.Other.OfType<XAttribute>())
                    {
                        bptElement.SetAttributeValue(attr.Name, attr.Value);
                    }

                    if (!string.IsNullOrEmpty(st.Value))
                        bptElement.Add(st.Value);

                    element.Add(bptElement);
                    processedParts.Add(st);
                }
                else if (part is EndTag et && !processedParts.Contains(et))
                {
                    if (et.StartTag == null || !et.StartTag.WellFormed)
                    {
                        var eptElement = new XElement(XliffNs + "ept");
                        if (!string.IsNullOrEmpty(et.Id))
                            eptElement.SetAttributeValue("id", et.Id);

                        foreach (var attr in tag.Other.OfType<XAttribute>())
                        {
                            eptElement.SetAttributeValue(attr.Name, attr.Value);
                        }

                        if (!string.IsNullOrEmpty(et.Value))
                            eptElement.Add(et.Value);

                        element.Add(eptElement);
                        processedParts.Add(et);
                    }
                }
                else if (!processedParts.Contains(tag))
                {
                    var phElement = new XElement(XliffNs + "ph");
                    if (!string.IsNullOrEmpty(tag.Id))
                        phElement.SetAttributeValue("id", tag.Id);

                    foreach (var attr in tag.Other.OfType<XAttribute>())
                    {
                        phElement.SetAttributeValue(attr.Name, attr.Value);
                    }

                    if (!string.IsNullOrEmpty(tag.Value))
                        phElement.Add(tag.Value);

                    element.Add(phElement);
                    processedParts.Add(tag);
                }
            }
            else if (part is AnnotationStart anno)
            {
                if (anno.WellFormed && anno.EndAnnotationReference != null)
                {
                    var mrkElement = new XElement(XliffNs + "mrk");

                    if (!string.IsNullOrEmpty(anno.Id))
                        mrkElement.SetAttributeValue("mid", anno.Id);

                    mrkElement.SetAttributeValue("mtype", !string.IsNullOrEmpty(anno.Type) ? anno.Type : "x-annotation");

                    int startIndex = parts.IndexOf(anno);
                    int endIndex = parts.IndexOf(anno.EndAnnotationReference);
                    if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
                    {
                        for (int i = startIndex + 1; i < endIndex; i++)
                        {
                            if (parts[i] is TextPart textPart && !processedParts.Contains(textPart))
                            {
                                mrkElement.Add(textPart.Value);
                                processedParts.Add(textPart);
                            }
                        }
                    }

                    element.Add(mrkElement);
                    processedParts.Add(anno);
                    processedParts.Add(anno.EndAnnotationReference);
                }
            }
            else if (!(part is AnnotationEnd) && !processedParts.Contains(part))
            {
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

        var sourceLanguage = xliffNode.Elements(XliffNs + "file").FirstOrDefault()?.Get("source-language");
        var targetLanguage = xliffNode.Elements(XliffNs + "file").FirstOrDefault()?.Get("target-language");

        var transformation = new Transformation(sourceLanguage, targetLanguage);
        var fileElements = xliffNode.Elements(XliffNs + "file").ToList();
        if (fileElements.Count == 1)
        {
            var fileElement = fileElements[0];
            var fileTransformation = DeserializeTransformation(fileElement, sourceLanguage ?? "en", targetLanguage ?? "en");
            transformation = fileTransformation;
        }
        else
        {
            foreach (var fileElement in fileElements)
            {
                var fileTransformation = DeserializeTransformation(fileElement, sourceLanguage ?? "en", targetLanguage ?? "en");
                transformation.Children.Add(fileTransformation);
            }
        }
        
        transformation.XliffOther.AddRange(xliffNode.Attributes().GetRemaining(["source-language", "target-language", "version"]));
        return transformation;
    }

    private static Transformation DeserializeTransformation(XElement fileElement, string sourceLanguage, string targetLanguage)
    {
        var fileTransformation = new Transformation(fileElement.Get("source-language") ?? sourceLanguage, fileElement.Get("target-language") ?? targetLanguage)
        {
            Id = fileElement.Get("id"),
            ExternalReference = fileElement.Get("original")
        };

        fileTransformation.Other.AddRange(fileElement.Attributes().GetRemaining(["id", "source-language", "target-language", "original"]));
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
                    fileTransformation.SkeletonOther = internalFile.Elements().ToList();
                }

                var externalFile = skeleton.Element(XliffNs + "external-file");
                if (externalFile != null)
                {
                    fileTransformation.OriginalReference = externalFile.Get("href");
                }
            }

            foreach (var note in header.Elements(XliffNs + "note"))
            {
                fileTransformation.Notes.Add(new Note(note.Value)
                {
                    Id = note.Get("id"),
                    Priority = int.TryParse(note.Get("priority"), out var priority) ? priority : null
                });
            }

            foreach (var note in header.Elements().Where(x => x.Name.LocalName != "note" && x.Name.LocalName != "skl"))
            {
                fileTransformation.Other.Add(note);
            }
        }

        var body = fileElement.Element(XliffNs + "body");
        if (body != null)
        {
            ProcessBodyContent(body, fileTransformation);
        }

        return fileTransformation;
    }

    private static void ProcessBodyContent(XElement body, Node parent)
    {
        foreach (var element in body.Elements())
        {
            if (element.Name == XliffNs + "group")
            {
                var group = new Group
                {
                    Id = element.Get("id"),
                    Name = element.Get("resname")
                };

                foreach (var note in element.Elements(XliffNs + "note"))
                {
                    group.Notes.Add(new Note(note.Value)
                    {
                        Id = note.Get("id")
                    });
                }

                ProcessBodyContent(element, group);

                if (parent is Group parentGroup)
                    parentGroup.Children.Add(group);
                else if (parent is Transformation transformation)
                    transformation.Children.Add(group);
            }
            else if (element.Name == XliffNs + "trans-unit")
            {
                var unit = new Unit
                {
                    Id = element.Get("id"),
                    Name = element.Get("resname")
                };

                var source = element.Element(XliffNs + "source");
                var target = element.Element(XliffNs + "target");
                var segSource = element.Element(XliffNs + "seg-source");
                var codeType = element.GetCodeType(BlackbirdNs + "tagHandling");

                if (segSource != null)
                {
                    foreach (var mrkElement in segSource.Elements(XliffNs + "mrk").Where(m => m.Get("mtype") == "seg"))
                    {
                        var segment = new Segment
                        {
                            Id = mrkElement.Get("mid"),
                            Source = ExtractTextParts(mrkElement),
                            CodeType = codeType
                        };

                        if (element.Get("translate") == "no")
                        {
                            segment.Ignorable = true;
                        }

                        if (target != null)
                        {
                            var midValue = mrkElement.Get("mid");
                            var matchingTargetMrk = target.Elements(XliffNs + "mrk")
                                .FirstOrDefault(m => m.Get("mtype") == "seg" &&
                                                    m.Get("mid") == midValue);

                            if (matchingTargetMrk != null)
                            {
                                segment.Target = ExtractTextParts(matchingTargetMrk);
                                var canResegmentAttr = matchingTargetMrk.Get(BlackbirdNs + "canResegment");
                                if (!string.IsNullOrEmpty(canResegmentAttr))
                                {
                                    segment.CanResegment = bool.Parse(canResegmentAttr);
                                }

                                var stateAttr = matchingTargetMrk.Get("state");
                                if (!string.IsNullOrEmpty(stateAttr))
                                {
                                    segment.TargetAttributes.Add(new XAttribute(BlackbirdNs + "customState", stateAttr));
                                    var target12State = stateAttr.ToTarget12State();
                                    if (target12State.HasValue)
                                    {
                                        segment.State = target12State.Value.ToSegmentState();
                                    }
                                }
                                else if (element.Get("approved") == "yes")
                                {
                                    segment.State = SegmentState.Final;
                                }
                            }
                        }

                        unit.Segments.Add(segment);
                    }
                }
                else
                {
                    var segment = new Segment
                    {
                        Source = source != null ? ExtractTextParts(source) : new List<TextPart>(),
                        Target = target != null ? ExtractTextParts(target) : new List<TextPart>(),
                        CodeType = codeType,
                        SourceAttributes = source?.Attributes().ToList() ?? new List<XAttribute>(),
                        TargetAttributes = target?.Attributes().ToList() ?? new List<XAttribute>(),
                        SourceWhiteSpaceHandling = source?.Get(XNamespace.Xml + "space") == "preserve" ? WhiteSpaceHandling.Preserve : WhiteSpaceHandling.Default,
                        TargetWhiteSpaceHandling = target?.Get(XNamespace.Xml + "space") == "preserve" ? WhiteSpaceHandling.Preserve : WhiteSpaceHandling.Default
                    };

                    var canResegmentAttr = target?.Get(BlackbirdNs + "canResegment");
                    if (!string.IsNullOrEmpty(canResegmentAttr))
                    {
                        segment.CanResegment = bool.Parse(canResegmentAttr);
                    }

                    if (element.Get("approved") == "yes")
                    {
                        segment.State = SegmentState.Final;
                    }
                    else if (target != null)
                    {
                        var stateAttr = target.Get("state");
                        if (!string.IsNullOrEmpty(stateAttr))
                        {
                            segment.TargetAttributes.Add(new XAttribute(BlackbirdNs + "customState", stateAttr));
                            var target12State = stateAttr.ToTarget12State();
                            if (target12State.HasValue)
                            {
                                segment.State = target12State.Value.ToSegmentState();
                            }
                            else
                            {
                                segment.State = SegmentState.Translated;
                            }
                        }
                        else
                        {
                            segment.State = SegmentState.Translated;
                        }
                    }

                    if (element.Get("phase-name") != null)
                        segment.SubState = element.Get("phase-name");

                    if (element.Get("translate") == "no")
                        segment.Ignorable = true;


                    unit.Segments.Add(segment);
                }

                foreach (var note in element.Elements(XliffNs + "note"))
                {
                    unit.Notes.Add(new Note(note.Value)
                    {
                        Id = note.Get("id")
                    });
                }

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
                    var startTag = new StartTag(true) { Id = childElement.Get("id") ?? idGenerator(null) };
                    var endTag = new EndTag { StartTag = startTag };
                    startTag.EndTag = endTag;
                    startTag.Other.AddRange(childElement.Attributes().GetRemaining(["id"]));

                    parts.Add(startTag);
                    parts.AddRange(ExtractTextParts(childElement));
                    parts.Add(endTag);
                }
                else if (childElement.Name == XliffNs + "bpt")
                {
                    var startTag = new StartTag
                    {
                        Id = childElement.Get("id") ?? idGenerator(null),
                        Value = childElement.Value
                    };

                    foreach (var attr in childElement.Attributes())
                    {
                        if (attr.Name.LocalName != "id")
                        {
                            startTag.Other.Add(attr);
                        }
                    }

                    parts.Add(startTag);
                }
                else if (childElement.Name == XliffNs + "ept")
                {
                    var endTag = new EndTag
                    {
                        Id = childElement.Get("id"),
                        Value = childElement.Value
                    };

                    foreach (var attr in childElement.Attributes())
                    {
                        if (attr.Name.LocalName != "id")
                        {
                            endTag.Other.Add(attr);
                        }
                    }

                    var ridValue = childElement.Get("rid");
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
                    var inlineTag = new InlineTag
                    {
                        Id = childElement.Get("id") ?? idGenerator(null),
                        Value = childElement.Value
                    };

                    foreach (var attr in childElement.Attributes())
                    {
                        if (attr.Name.LocalName != "id")
                        {
                            inlineTag.Other.Add(attr);
                        }
                    }

                    parts.Add(inlineTag);
                }
                else if (childElement.Name == XliffNs + "mrk")
                {
                    var mtype = childElement.Get("mtype");
                    if (mtype != "seg") // Skip segmentation marks
                    {
                        var annotation = new AnnotationStart(true)
                        {
                            Id = childElement.Get("mid") ?? idGenerator(null),
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

            if (xliffNode.Name.Namespace != XliffNs || xliffNode.Name.LocalName != "xliff")
            {
                return false;
            }

            var version = xliffNode.Get("version");
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            return version.StartsWith("1.");
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

    private static string CompactSourceElements(string xmlString)
    {
        var lines = xmlString.Split('\n');
        var result = new List<string>();
        bool inSourceElement = false;
        var sourceContent = new StringBuilder();
        string sourceIndent = "";

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("<source"))
            {
                inSourceElement = true;
                sourceContent.Clear();
                sourceContent.Append(trimmedLine);

                sourceIndent = line.Substring(0, line.IndexOf('<'));
                if (trimmedLine.EndsWith("</source>") || trimmedLine.EndsWith("/>"))
                {
                    result.Add(line);
                    inSourceElement = false;
                }
            }
            else if (inSourceElement)
            {
                if (trimmedLine.EndsWith("</source>"))
                {
                    sourceContent.Append(trimmedLine);
                    result.Add(sourceIndent + sourceContent);
                    inSourceElement = false;
                }
                else
                {
                    sourceContent.Append(trimmedLine);
                }
            }
            else
            {
                result.Add(line);
            }
        }

        return string.Join("\n", result);
    }
}