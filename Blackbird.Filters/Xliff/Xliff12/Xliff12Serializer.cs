using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Annotation;
using Blackbird.Filters.Transformations.Tags;
using System.Text;
using System.Xml.Linq;
using Blackbird.Filters.Xliff.Xliff2;
using System.Reflection.PortableExecutable;

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

    private static XElement? CloneWithNamespace(XObject? xobj)
    {
        if (xobj == null) return null;

        if (xobj is XElement element)
        {
            if (element.Name.Namespace == "urn:oasis:names:tc:xliff:metadata:2.0" || element.Name.Namespace == "urn:oasis:names:tc:xliff:validation:2.0")
            {
                return element;
            }
            
            // Preserve elements that are already in a non-empty namespace (like BlackbirdNs)
            var newElement = new XElement(
                element.Name.Namespace != XNamespace.None && !element.Name.LocalName.StartsWith("xliff:") ? element.Name :
                element.Name.LocalName.StartsWith("xliff:") ? element.Name : 
                XliffNs + element.Name.LocalName);

            foreach (var attr in element.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                    continue;
                
                if (attr.Name.Namespace == XNamespace.None || attr.Name.Namespace == XNamespace.Xmlns)
                    newElement.SetAttributeValue(attr.Name, attr.Value);
                else
                    newElement.SetAttributeValue(attr.Name, attr.Value);
            }

            // Process child nodes recursively
            foreach (var node in element.Nodes())
            {
                if (node is XElement childElement)
                {
                    newElement.Add(CloneWithNamespace(childElement));
                }
                else if (node is XText textNode)
                {
                    newElement.Add(new XText(textNode.Value));
                }
            }

            return newElement;
        }

        return null;
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
            
            fileElement.Set("target-language", file.TargetLanguage);
            fileElement.Set("original", file.ExternalReference);
            fileElement.SetBool(BlackbirdNs + "canResegment", file.CanResegment);
            fileElement.SetBool(BlackbirdNs + "translate", file.Translate);
            fileElement.SetDirection(BlackbirdNs + "srcDir", file.SourceDirection);
            fileElement.SetDirection(BlackbirdNs + "trgDir", file.TargetDirection);

            var header = new XElement(XliffNs + "header");
            if (file.Notes.Count > 0)
            {
                foreach (var note in file.Notes)
                {
                    var noteElement = new XElement(XliffNs + "note", note.Text);
                    noteElement.Set("id", note.Id);
                    noteElement.SetInt("priority", note.Priority);
                    noteElement.Set(BlackbirdNs + "category", note.Category);
                    noteElement.Set(BlackbirdNs + "reference", note.Reference);
                    noteElement.SetLanguageTarget(BlackbirdNs + "languageTarget", note.LanguageTarget);
                    foreach (var attr in note.Other)
                    {
                        if (!attr.IsNamespaceDeclaration)
                        {
                            noteElement.Set(attr.Name, attr.Value);
                        }
                    }
                    
                    header.Add(noteElement);
                }
            }

            if (!string.IsNullOrEmpty(file.Original) || !string.IsNullOrEmpty(file.OriginalReference))
            {
                var skeleton = new XElement(XliffNs + "skl");

                if (!string.IsNullOrEmpty(file.OriginalReference))
                {
                    var externalFile = new XElement(XliffNs + "external-file",
                        new XAttribute("href", file.OriginalReference));
                    
                    var externalFileAttrs = file.SkeletonOther.OfType<XElement>()
                        .FirstOrDefault(x => x.Name.LocalName == "external-file")?.Attributes();
                    
                    if (externalFileAttrs != null)
                    {
                        foreach (var attr in externalFileAttrs)
                        {
                            if (attr.Name.LocalName != "href" && !attr.IsNamespaceDeclaration)
                            {
                                externalFile.SetAttributeValue(attr.Name, attr.Value);
                            }
                        }
                    }
                    
                    skeleton.Add(externalFile);
                }
                else if (!string.IsNullOrEmpty(file.Original))
                {
                    var internalFile = new XElement(XliffNs + "internal-file");
                    
                    // Add any form attribute if present
                    var internalFileAttrs = file.SkeletonOther.OfType<XElement>()
                        .FirstOrDefault(x => x.Name.LocalName == "internal-file")?.Attributes();
                    
                    if (internalFileAttrs != null)
                    {
                        foreach (var attr in internalFileAttrs)
                        {
                            if (!attr.IsNamespaceDeclaration)
                            {
                                internalFile.SetAttributeValue(attr.Name, attr.Value);
                            }
                        }
                    }
                    
                    internalFile.Add(new XText(file.Original));
                    
                    // Handle SkeletonOther elements with namespace correction
                    foreach (var elem in file.SkeletonOther)
                    {
                        if (elem is XElement skelElement && skelElement.Name.LocalName != "internal-file")
                        {
                            var clonedSkelElement = CloneWithNamespace(skelElement);
                            if (clonedSkelElement != null)
                                internalFile.Add(clonedSkelElement);
                        }
                    }
                    
                    skeleton.Add(internalFile);
                }

                header.Add(skeleton);
            }
            
            foreach (var otherElements in file.Other)
            {
                if (otherElements is XElement otherElement)
                {
                    var clonedElement = CloneWithNamespace(otherElement);
                    if (clonedElement != null)
                        header.Add(clonedElement);
                }
                else if (otherElements is XAttribute attribute)
                {
                    // Don't add namespace attributes to avoid conflicts
                    if (!attribute.IsNamespaceDeclaration)
                        fileElement.SetAttributeValue(attribute.Name, attribute.Value);
                }
            }

            fileElement.Add(header);

            var body = new XElement(XliffNs + "body");
            SerializeGroupsAndUnits(file, body);
            fileElement.Add(body);

            return fileElement;
        }

        try
        {
            foreach (var otherObj in transformation.XliffOther)
            {
                if (otherObj is XAttribute attr)
                {
                    if (!attr.Value.Contains("urn:oasis:names:tc:xliff:document") && attr.Name.LocalName != "version")
                        root.SetAttributeValue(attr.Name, attr.Value);
                }
                else if (otherObj is XElement elem)
                {
                    var cloned = CloneWithNamespace(elem);
                    if (cloned != null)
                        root.Add(cloned);
                }
            }
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

    private static IEnumerable<XElement> SerializeNotes(List<Note> notes)
    {
        foreach (var note in notes)
        {
            var noteElement = new XElement(XliffNs + "note", note.Text);
            noteElement.Set("id", note.Id);
            noteElement.SetInt("priority", note.Priority);
            noteElement.Set(BlackbirdNs + "category", note.Category);
            noteElement.Set(BlackbirdNs + "reference", note.Reference);
            noteElement.SetLanguageTarget(BlackbirdNs + "languageTarget", note.LanguageTarget);
            foreach (var attr in note.Other)
            {
                if (!attr.IsNamespaceDeclaration)
                {
                    noteElement.SetAttributeValue(attr.Name, attr.Value);
                }
            }
            yield return noteElement;
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
            groupElement.Set("resname", group.Name);
            groupElement.SetBool(BlackbirdNs + "canResegment", group.CanResegment);
            groupElement.SetBool(BlackbirdNs + "translate", group.Translate);
            groupElement.SetDirection(BlackbirdNs + "srcDir", group.SourceDirection);
            groupElement.SetDirection(BlackbirdNs + "trgDir", group.TargetDirection);
            foreach (var attr in group.Other.OfType<XAttribute>().GetRemaining(["resname", "canResegment", "translate", "srcDir", "trgDir"]).Where(attr => !attr.IsNamespaceDeclaration))
            {
                groupElement.SetAttributeValue(attr.Name, attr.Value);
            }

            // Add notes first
            foreach (var note in group.Notes)
            {
                var noteElement = new XElement(XliffNs + "note", note.Text);
                noteElement.Set("id", note.Id);
                noteElement.SetInt("priority", note.Priority);
                noteElement.Set(BlackbirdNs + "category", note.Category);
                noteElement.Set(BlackbirdNs + "reference", note.Reference);
                noteElement.SetLanguageTarget(BlackbirdNs + "languageTarget", note.LanguageTarget);
                foreach (var attr in note.Other)
                {
                    if (!attr.IsNamespaceDeclaration)
                    {
                        noteElement.SetAttributeValue(attr.Name, attr.Value);
                    }
                }
                
                groupElement.Add(noteElement);
            }

            // Add other elements (context-group, count-group, prop-group, etc.)
            foreach (var otherElement in group.Other.OfType<XElement>())
            {
                var clonedElement = CloneWithNamespace(otherElement);
                if (clonedElement != null)
                    groupElement.Add(clonedElement);
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
            
            transUnit.Set("resname", unit.Name);
            transUnit.SetBool(BlackbirdNs + "canResegment", unit.CanResegment);
            transUnit.SetBool(BlackbirdNs + "translate", unit.Translate);
            transUnit.SetDirection(BlackbirdNs + "srcDir", unit.SourceDirection);
            transUnit.SetDirection(BlackbirdNs + "trgDir", unit.TargetDirection);
            foreach (var attr in unit.Other.OfType<XAttribute>().GetRemaining(["resname", "canResegment", "translate", "srcDir", "trgDir"]).Where(attr => !attr.IsNamespaceDeclaration))
            {
                transUnit.SetAttributeValue(attr.Name, attr.Value);
            }

            if (unit.Segments.Count == 1)
            {
                var segment = unit.Segments[0];
                transUnit.Add(SerializeTextParts(segment.Source, "source", segment.SourceAttributes));
                transUnit.Set(BlackbirdNs + "segmentId", segment.Id);

                if (segment.Target.Any())
                {
                    var targetElement = SerializeTextParts(segment.Target, "target", segment.TargetAttributes);
                    targetElement.SetBool(BlackbirdNs + "canResegment", segment.CanResegment);
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
                foreach (var segment in unit.Segments.Where(x => !string.IsNullOrEmpty(x.Id)))
                {
                    var mrkElement = SerializeTextParts(segment.Source, "mrk", null);
                    mrkElement.SetAttributeValue("mtype", "seg");
                    mrkElement.SetAttributeValue("mid", segment.Id);
                    segSource.Add(mrkElement);
                    
                    if(segment.SourceAttributes.Count > 0)
                    {
                        foreach (var attr in segment.SourceAttributes.GetRemaining(["space"]))
                        {
                            segSource.Set(attr.Name, attr.Value);
                        }
                    }
                }

                transUnit.Add(segSource);
                if (unit.Segments.Any(s => s.Target.Any()))
                {
                    var target = new XElement(XliffNs + "target");
                    foreach (var segment in unit.Segments.Where(x => !string.IsNullOrEmpty(x.Id)))
                    {
                        foreach (var attr in segment.TargetAttributes)
                        {
                            target.Set(attr.Name, attr.Value);
                        }
                        
                        if (segment.Target.Any())
                        {
                            var mrkElement = SerializeTextParts(segment.Target, "mrk", segment.TargetAttributes);
                            mrkElement.SetAttributeValue("mtype", "seg");
                            mrkElement.SetAttributeValue("mid", segment.Id);
                            mrkElement.SetBool(BlackbirdNs + "canResegment", segment.CanResegment);
                            if (segment.State.HasValue)
                            {
                                var state12 = segment.State.Value.ToTarget12State()?.Serialize();
                                if (state12 != null)
                                {
                                    mrkElement.Set("state", segment.State.Value.ToTarget12State()?.Serialize());
                                }
                            }
                            mrkElement.Set("phase-name", segment.SubState);
                            mrkElement.SetBool(BlackbirdNs + "ignorable", segment.Ignorable);
                            mrkElement.SetInt(BlackbirdNs + "order", segment.Order);

                            if (segment.State is SegmentState.Final)
                            {
                                if (unit.Segments.All(s => s.State is SegmentState.Final))
                                {
                                    transUnit.SetAttributeValue("approved", "yes");
                                }
                            }
                            target.Add(mrkElement);
                        }
                    }

                    transUnit.Add(target);
                }
            }

            transUnit.SetCodeType(BlackbirdNs + "tagHandling", unit.Segments.FirstOrDefault()?.CodeType);

            // Add other elements (context-group, count-group, prop-group, alt-trans, etc.) first
            foreach (var otherElement in unit.Other.OfType<XElement>())
            {
                var clonedElement = CloneWithNamespace(otherElement);
                if (clonedElement != null)
                    transUnit.Add(clonedElement);
            }

            // Add notes last
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

    private static XElement SerializeTextParts(List<TextPart> parts, string elementName, IEnumerable<XAttribute>? attributes = null)
    {
        var element = new XElement(XliffNs + elementName);
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
                    phElement.Set("id", tag.Id);

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
                var mrkElement = new XElement(XliffNs + "mrk");

                mrkElement.Set("mid", anno.Id);
                mrkElement.SetBool(BlackbirdNs + "translate", anno.Translate);
                mrkElement.Set(BlackbirdNs + "ref", anno.Ref);
                mrkElement.Set(BlackbirdNs + "value", anno.AttributeValue);
                mrkElement.Add(anno.Other);
                mrkElement.Set("mtype", anno.Type);

                if (!anno.WellFormed)
                {
                    mrkElement.Set(BlackbirdNs + "position", "x-start");
                }
                else if (anno.EndAnnotationReference is not null)
                {
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

                    processedParts.Add(anno);
                    processedParts.Add(anno.EndAnnotationReference);
                }

                element.Add(mrkElement);

            }
            else if (part is AnnotationEnd endAnno)
            {
                var mrkElement = new XElement(XliffNs + "mrk");
                mrkElement.Set(BlackbirdNs + "position", "x-end");

                if (endAnno.StartAnnotationReference is not null)
                {
                    mrkElement.Set(BlackbirdNs + "start-ref", endAnno.StartAnnotationReference.Id);
                }
                element.Add(mrkElement);
            }
            else if (!(part is AnnotationEnd) && !processedParts.Contains(part))
            {
                element.Add(ReplaceInvalidXmlChars(part.Value));
            }
        }

        return element;
    }

    private static IEnumerable<XNode> ReplaceInvalidXmlChars(string input)
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
                // Todo: this ph needs an id
                nodes.Add(new XElement(XliffNs + "ph", new XAttribute(BlackbirdNs + "x-hex", cp.ToString("X4"))));
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
            var fileTransformation = DeserializeTransformation(fileElement, sourceLanguage, targetLanguage);
            transformation = fileTransformation;
        }
        else
        {
            foreach (var fileElement in fileElements)
            {
                var fileTransformation = DeserializeTransformation(fileElement, sourceLanguage, targetLanguage);
                transformation.Children.Add(fileTransformation);
            }
        }
        
        transformation.XliffOther.AddRange(xliffNode.Attributes().GetRemaining(["source-language", "target-language", "version"]));
        return transformation;
    }

    private static Transformation DeserializeTransformation(XElement fileElement, string? sourceLanguage, string? targetLanguage)
    {
        var fileTransformation = new Transformation(fileElement.Get("source-language") ?? sourceLanguage, fileElement.Get("target-language") ?? targetLanguage)
        {
            Id = fileElement.Get("id"),
            ExternalReference = fileElement.Get("original"),
            CanResegment = fileElement.GetBool(BlackbirdNs + "canResegment"),
            Translate = fileElement.GetBool(BlackbirdNs + "translate"),
            SourceDirection = fileElement.GetDirection(BlackbirdNs + "srcDir"),
            TargetDirection = fileElement.GetDirection(BlackbirdNs + "trgDir")
        };
        
        var other = fileElement.Attributes().Where(a => a.Name.Namespace != BlackbirdNs && a.Value != BlackbirdNs.NamespaceName).ToList();
        fileTransformation.Other.AddRange(other.GetRemaining(["id", "source-language", "target-language", "original"]));
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

            fileTransformation.Notes = DeserializeNotes(header.Elements(XliffNs + "note"));

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

    private static List<Note> DeserializeNotes(IEnumerable<XElement> elements)
    {
        var notes = new List<Note>();
        foreach (var note in elements)
        {
            notes.Add(new Note(note.Value)
            {
                Id = note.Get("id"),
                Priority = note.GetInt("priority"),
                Category = note.Get(BlackbirdNs + "category"),
                Reference = note.Get(BlackbirdNs + "reference"),
                LanguageTarget = note.GetLanguageTarget(BlackbirdNs + "languageTarget"),
                Other = note.Attributes().GetRemaining(["id", "priority", "category", "reference"]).Where(a => a.Name.Namespace != BlackbirdNs && a.Name.Namespace != XliffNs).ToList()
            });
        }
        return notes;
    }

    private static void ProcessBodyContent(XElement body, Node parent)
    {
        foreach (var element in body.Elements())
        {
            if (element.Name == XliffNs + "group")
            {
                var group = new Group
                {
                    //id="g1" canResegment="yes" translate="yes" srcDir="ltr" trgDir="ltr" name="group1" type="z:typeg" my:attr="value4"
                    Id = element.Get("id"),
                    Name = element.Get("resname"),
                    CanResegment = element.GetBool(BlackbirdNs + "canResegment"),
                    Translate = element.GetBool(BlackbirdNs + "translate"),
                    SourceDirection = element.GetDirection(BlackbirdNs + "srcDir"),
                    TargetDirection = element.GetDirection(BlackbirdNs + "trgDir"),
                    Type = element.Get(BlackbirdNs + "type")
                };
                
                var other = element.Attributes().Where(a => a.Name.Namespace != BlackbirdNs && a.Name.Namespace != XliffNs).ToList();
                group.Other.AddRange(other.GetRemaining(["id", "resname", "canResegment", "translate", "srcDir", "trgDir"]));
                
                foreach (var note in element.Elements(XliffNs + "note"))
                {
                    group.Notes.Add(new Note(note.Value)
                    {
                        Id = note.Get("id"),
                        Priority = note.GetInt("priority"),
                        Category = note.Get(BlackbirdNs + "category"),
                        Reference = note.Get(BlackbirdNs + "reference"),
                        LanguageTarget = note.GetLanguageTarget(BlackbirdNs + "languageTarget"),
                        Other = note.Attributes().GetRemaining(["id", "priority", "category", "reference"]).Where(a => a.Name.Namespace != BlackbirdNs && a.Name.Namespace != XliffNs).ToList()
                    });
                }

                // Add other elements (context-group, count-group, prop-group, etc.) to Other property
                var otherElements = element.Elements().Where(e => e.Name.LocalName != "note" && e.Name.LocalName != "group" && e.Name.LocalName != "trans-unit").ToList();
                group.Other.AddRange(otherElements);

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
                    Name = element.Get("resname"),
                    CanResegment = element.GetBool(BlackbirdNs + "canResegment"),
                    Translate = element.GetBool(BlackbirdNs + "translate"),
                    SourceDirection = element.GetDirection(BlackbirdNs + "srcDir"),
                    TargetDirection = element.GetDirection(BlackbirdNs + "trgDir")
                };
                
                var other = element.Attributes().Where(a => a.Name.Namespace != BlackbirdNs && a.Name.Namespace != XliffNs).ToList();
                unit.Other.AddRange(other.GetRemaining(["id", "resname", "canResegment", "translate", "srcDir", "trgDir"]));

                // Add other elements (context-group, count-group, prop-group, alt-trans, etc.) to Other property
                var otherElements = element.Elements().Where(e => e.Name.LocalName != "source" && e.Name.LocalName != "target" && e.Name.LocalName != "seg-source" && e.Name.LocalName != "mrk" && e.Name.LocalName != "note").ToList();
                unit.Other.AddRange(otherElements);

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
                                segment.CanResegment = matchingTargetMrk.GetBool(BlackbirdNs + "canResegment");
                                segment.Order = matchingTargetMrk.GetInt(BlackbirdNs + "order");
                                segment.SubState = matchingTargetMrk.Get("phase-name");
                                segment.Ignorable = matchingTargetMrk.GetBool(BlackbirdNs + "ignorable");
                                segment.TargetAttributes = matchingTargetMrk.Attributes().GetRemaining(["mtype", "mid", "phase-name", "canResegment", "ignorable", "order", "state"]).Where(x => x.Name.Namespace != BlackbirdNs).ToList();

                                var stateAttr = matchingTargetMrk.Get("state");
                                if (!string.IsNullOrEmpty(stateAttr))
                                {
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
                        Id = element.Get(BlackbirdNs + "segmentId"),
                        Source = source != null ? ExtractTextParts(source) : new List<TextPart>(),
                        Target = target != null ? ExtractTextParts(target) : new List<TextPart>(),
                        CodeType = codeType,
                        SourceAttributes = source?.Attributes().ToList() ?? new List<XAttribute>(),
                        TargetAttributes = target?.Attributes().ToList() ?? new List<XAttribute>()
                    };

                    var sourceWhiteSpaceHandling = source?.Get(XNamespace.Xml + "space");
                    if (!string.IsNullOrEmpty(sourceWhiteSpaceHandling))
                    {
                        segment.SourceWhiteSpaceHandling = sourceWhiteSpaceHandling == "preserve" ? WhiteSpaceHandling.Preserve : WhiteSpaceHandling.Default;
                    }
                    
                    var targetWhiteSpaceHandling = target?.Get(XNamespace.Xml + "space");
                    if (!string.IsNullOrEmpty(targetWhiteSpaceHandling))
                    {
                        segment.TargetWhiteSpaceHandling = targetWhiteSpaceHandling == "preserve" ? WhiteSpaceHandling.Preserve : WhiteSpaceHandling.Default;
                    }
                    
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
                        }
                    }

                    if (element.Get("phase-name") != null)
                        segment.SubState = element.Get("phase-name");

                    if (element.Get("translate") == "no")
                        segment.Ignorable = true;


                    unit.Segments.Add(segment);
                }

                unit.Notes = DeserializeNotes(element.Elements(XliffNs + "note"));

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
                        var type = childElement.Get("type");
                        var position = childElement.Get(BlackbirdNs + "position");
                        if (position == "x-start")
                        {
                            var annotation = new AnnotationStart(false)
                            {
                                Id = childElement.Get("mid") ?? idGenerator(null),
                                Type = mtype,
                                Translate = childElement.GetBool(BlackbirdNs + "translate"),
                                Ref = childElement.Get(BlackbirdNs + "ref"),
                                AttributeValue = childElement.Get(BlackbirdNs + "value"),
                                Other = childElement.Attributes().GetRemaining(["mid", BlackbirdNs + "translate", "mtype", BlackbirdNs + "value", BlackbirdNs + "ref", BlackbirdNs + "position"]),
                            };
                            parts.Add(annotation);
                        }
                        else if (position == "x-end")
                        {
                            var startRef = childElement.Get(BlackbirdNs + "start-ref", Optionality.Required);
                            var matchingStartAnnotation = parts.OfType<AnnotationStart>().FirstOrDefault(x => x.Id == startRef);
                            var annotation = new AnnotationEnd()
                            {
                                StartAnnotationReference = matchingStartAnnotation
                            };
                            parts.Add(annotation);
                        }
                        else
                        {
                            var annotation = new AnnotationStart(true)
                            {
                                Id = childElement.Get("mid") ?? idGenerator(null),
                                Type = mtype,
                                Translate = childElement.GetBool(BlackbirdNs + "translate"),
                                Ref = childElement.Get(BlackbirdNs + "ref"),
                                AttributeValue = childElement.Get(BlackbirdNs + "value"),
                                Other = childElement.Attributes().GetRemaining(["mid", BlackbirdNs + "translate", "mtype", BlackbirdNs + "value", BlackbirdNs + "ref", BlackbirdNs + "position"]),
                            };

                            var endAnnotation = new AnnotationEnd { StartAnnotationReference = annotation };
                            annotation.EndAnnotationReference = endAnnotation;

                            parts.Add(annotation);
                            parts.AddRange(ExtractTextParts(childElement));
                            parts.Add(endAnnotation);
                        }
                        
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