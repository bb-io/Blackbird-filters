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
    private static readonly XName HasSegSourceAttrName = BlackbirdNs + "hasSegSource";
    private static readonly XNamespace XliffNs = "urn:oasis:names:tc:xliff:document:1.2";

    public static string Serialize(Transformation transformation)
    {
        var root = new XElement(XliffNs + "xliff",
            new XAttribute("version", "1.2"));

        SerializeTransformation(transformation, root);

        var doc = new XDocument(root);
        var xmlString = doc.ToString();

        return Xliff12XmlExtensions.CompactSourceElements(xmlString);
    }
    
    public static Transformation Deserialize(string fileContent)
    {
        var xliffNode = Xliff12XmlExtensions.GetRootNode(fileContent);
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
    
    public static bool IsXliff12(string content)
    {
        try
        {
            var xliffNode = Xliff12XmlExtensions.GetRootNode(content);
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

    private static XElement? CloneWithNamespace(XObject? xobj)
    {
        if (xobj == null)
        {
            return null;
        }

        if (xobj is XElement element)
        {
            if (element.Name.Namespace == "urn:oasis:names:tc:xliff:metadata:2.0" || element.Name.Namespace == "urn:oasis:names:tc:xliff:validation:2.0")
            {
                return element;
            }
            
            var newElement = new XElement(
                element.Name.Namespace != XNamespace.None && !element.Name.LocalName.StartsWith("xliff:") ? element.Name :
                element.Name.LocalName.StartsWith("xliff:") ? element.Name : 
                XliffNs + element.Name.LocalName);

            foreach (var attr in element.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                    continue;
                
                if (attr.Name.Namespace == XNamespace.None || attr.Name.Namespace == XNamespace.Xmlns)
                {
                    newElement.SetAttributeValue(attr.Name, attr.Value);
                }
                else
                {
                    newElement.SetAttributeValue(attr.Name, attr.Value);
                }
            }

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
                    foreach (var elem in file.SkeletonOther)
                    {
                        if (elem is XElement skelElement && skelElement.Name.LocalName != "internal-file")
                        {
                            var clonedSkelElement = CloneWithNamespace(skelElement);
                            if (clonedSkelElement != null)
                            {
                                internalFile.Add(clonedSkelElement);
                            }
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
                    {
                        header.Add(clonedElement);
                    }
                }
                else if (otherElements is XAttribute { IsNamespaceDeclaration: false } attribute)
                {
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
                    {
                        root.SetAttributeValue(attr.Name, attr.Value);
                    }
                }
                else if (otherObj is XElement elem)
                {
                    var cloned = CloneWithNamespace(elem);
                    if (cloned != null)
                    {
                        root.Add(cloned);
                    }
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
            foreach (var attr in note.Other.Where(attr => !attr.IsNamespaceDeclaration))
            {
                noteElement.SetAttributeValue(attr.Name, attr.Value);
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
            groupElement.SetBool("translate", group.Translate);
            groupElement.SetDirection(BlackbirdNs + "srcDir", group.SourceDirection);
            groupElement.SetDirection(BlackbirdNs + "trgDir", group.TargetDirection);
            foreach (var attr in group.Other.OfType<XAttribute>().GetRemaining(["resname", "canResegment", "translate", "srcDir", "trgDir"]).Where(attr => !attr.IsNamespaceDeclaration))
            {
                groupElement.SetAttributeValue(attr.Name, attr.Value);
            }
            
            foreach (var note in group.Notes)
            {
                var noteElement = new XElement(XliffNs + "note", note.Text);
                noteElement.Set("id", note.Id);
                noteElement.SetInt("priority", note.Priority);
                noteElement.Set(BlackbirdNs + "category", note.Category);
                noteElement.Set(BlackbirdNs + "reference", note.Reference);
                noteElement.SetLanguageTarget(BlackbirdNs + "languageTarget", note.LanguageTarget);
                foreach (var attr in note.Other.Where(x => !x.IsNamespaceDeclaration))
                {
                    noteElement.SetAttributeValue(attr.Name, attr.Value);
                }
                
                groupElement.Add(noteElement);
            }

            foreach (var otherElement in group.Other.OfType<XElement>())
            {
                var clonedElement = CloneWithNamespace(otherElement);
                if (clonedElement != null)
                {
                    groupElement.Add(clonedElement);
                }
            }

            foreach (var child in group.Children)
            {
                if (child is Group childGroup)
                {
                    SerializeGroup(childGroup, groupElement);
                }
                else if (child is Unit unit)
                {
                    SerializeUnit(unit, groupElement);
                }
            }

            parentElement.Add(groupElement);
        }

        void SerializeUnit(Unit unit, XElement parentElement)
        {
            if (!unit.Segments.Any())
            {
                return;
            }

            unit.Id = unitId(unit.Id);
            var transUnit = new XElement(XliffNs + "trans-unit",
                new XAttribute("id", unit.Id));
            
            transUnit.Set("resname", unit.Name);
            transUnit.SetBool(BlackbirdNs + "canResegment", unit.CanResegment);
            if (unit.Translate.HasValue)
            {
                transUnit.Set("translate", unit.Translate.Value ? "yes" : "no");
            }
            
            transUnit.SetDirection(BlackbirdNs + "srcDir", unit.SourceDirection);
            transUnit.SetDirection(BlackbirdNs + "trgDir", unit.TargetDirection);
            foreach (var attr in unit.Other.OfType<XAttribute>().GetRemaining(["resname", "canResegment", "translate", "srcDir", "trgDir", HasSegSourceAttrName]).Where(attr => !attr.IsNamespaceDeclaration))
            {
                transUnit.SetAttributeValue(attr.Name, attr.Value);
            }

            var hasSegSource = unit.Other.OfType<XAttribute>().Any(a => a.Name == HasSegSourceAttrName && a.Value == "true");
            if (unit.Segments.Count == 1 && !hasSegSource)
            {
                var segment = unit.Segments[0];
                transUnit.Add(SerializeTextParts(segment.Source, "source", segment.SourceAttributes));
                if (segment.Target.Any())
                {
                    var targetElement = SerializeTextParts(segment.Target, "target", segment.TargetAttributes);
                    targetElement.SetBool(BlackbirdNs + "canResegment", segment.CanResegment);
                    if (segment.State != null)
                    {
                        targetElement.Set("state", segment.State.Value.ToTarget12State()?.Serialize());
                    }
                    
                    transUnit.Add(targetElement);
                }

                if (segment.SubState != null)
                {
                    transUnit.Set("phase-name", segment.SubState);
                }
            }
            else
            {
                var sourceContent = new StringBuilder();
                foreach (var segment in unit.Segments)
                {
                    sourceContent.Append(string.Join(string.Empty, segment.Source.Select(p => p.Value)));
                    var isLast = segment == unit.Segments.Last();
                    if (!isLast)
                    {
                        sourceContent.Append(' ');
                    }
                }

                transUnit.Add(new XElement(XliffNs + "source", sourceContent.ToString()));
                var segSource = new XElement(XliffNs + "seg-source");
                foreach (var segment in unit.Segments.Where(x => !string.IsNullOrEmpty(x.Id)))
                {
                    var mrkElement = SerializeTextParts(segment.Source, "mrk", null);
                    mrkElement.SetAttributeValue("mtype", "seg");
                    mrkElement.SetAttributeValue("mid", segment.Id);
                    segSource.Add(mrkElement);
                    
                    foreach (var attr in segment.SourceAttributes.GetRemaining(["space"]))
                    {
                        segSource.Set(attr.Name, attr.Value);
                    }
                }

                transUnit.Add(segSource);
                if (unit.Segments.Any(s => s.Target.Any()))
                {
                    var target = new XElement(XliffNs + "target");
                    var states = unit.Segments
                        .Where(s => s.State.HasValue)
                        .Select(s => s.State!.Value)
                        .Distinct()
                        .ToList();

                    var hasUniformState = states.Count == 1 && unit.Segments.All(s => s.State.HasValue);
                    if (hasUniformState)
                    {
                        var state = states.First().ToTarget12State()?.Serialize();
                        if (!string.IsNullOrEmpty(state))
                        {
                            target.Set("state", state);
                        }
                    }

                    var translateValue = unit.Translate.HasValue 
                        ? unit.Translate.Value ? "yes" : "no" 
                        : null;

                    bool? unitTranslate = translateValue switch
                    {
                        "yes" => true,
                        "no" => false,
                        _ => null
                    };

                    foreach (var segment in unit.Segments.Where(x => !string.IsNullOrEmpty(x.Id)))
                    {
                        if (segment.Target.Any())
                        {
                            var mrkElement = SerializeTextParts(segment.Target, "mrk", segment.TargetAttributes);
                            mrkElement.SetAttributeValue("mtype", "seg");
                            mrkElement.SetAttributeValue("mid", segment.Id);
                            mrkElement.SetBool(BlackbirdNs + "canResegment", segment.CanResegment);

                            mrkElement.Set("phase-name", segment.SubState);
                            if (segment.Ignorable == true && (!unitTranslate.HasValue || segment.Ignorable.Value != !unitTranslate.Value))
                            {
                                mrkElement.SetBool(BlackbirdNs + "ignorable", segment.Ignorable);
                            }

                            mrkElement.SetInt(BlackbirdNs + "order", segment.Order);
                            if (!hasUniformState)
                            {
                                var state = segment.State?.ToTarget12State()?.Serialize();
                                if (!string.IsNullOrEmpty(state))
                                {
                                    mrkElement.Set(BlackbirdNs + "customState", state);
                                }
                            }

                            target.Add(mrkElement);
                        }
                    }

                    transUnit.Add(target);
                }
            }

            transUnit.Set(BlackbirdNs + "tagHandling", unit.Segments.FirstOrDefault()?.OriginalMediaType);
            foreach (var noteElement in SerializeNotes(unit.Notes))
            {
                transUnit.Add(noteElement);
            }

            foreach (var otherElement in unit.Other.OfType<XElement>())
            {
                var clonedElement = CloneWithNamespace(otherElement);
                if (clonedElement != null)
                {
                    transUnit.Add(clonedElement);
                }
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
                {
                    SerializeGroup(childGroup, parent);
                }
                else if (child is Unit childUnit)
                {
                    SerializeUnit(childUnit, parent);
                }
                else if (child is Transformation childTransformation)
                {
                    SerializeGroupsAndUnits(childTransformation, parent);
                }
            }
        }
    }

    private static XElement SerializeTextParts(List<TextPart> parts, string elementName, IEnumerable<XAttribute>? attributes = null)
    {
        var element = new XElement(XliffNs + elementName);
        if (attributes != null)
        {
            foreach (var attr in attributes.GetRemaining([BlackbirdNs + "customState", BlackbirdNs + "canResegment"]))
            {
                element.SetAttributeValue(attr.Name, attr.Value);
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
                    {
                        gElement.SetAttributeValue("id", startTag.Id);
                    }

                    foreach (var attr in startTag.Other.OfType<XAttribute>())
                    {
                        gElement.SetAttributeValue(attr.Name, attr.Value);
                    }

                    var startIndex = parts.IndexOf(startTag);
                    var endIndex = parts.IndexOf(startTag.EndTag);
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
                    {
                        bptElement.SetAttributeValue("id", st.Id);
                    }

                    foreach (var attr in st.Other.OfType<XAttribute>())
                    {
                        bptElement.SetAttributeValue(attr.Name, attr.Value);
                    }

                    if (!string.IsNullOrEmpty(st.Value))
                    {
                        bptElement.Add(st.Value);
                    }

                    element.Add(bptElement);
                    processedParts.Add(st);
                }
                else if (part is EndTag et && !processedParts.Contains(et))
                {
                    if (et.StartTag == null || !et.StartTag.WellFormed)
                    {
                        var eptElement = new XElement(XliffNs + "ept");
                        if (!string.IsNullOrEmpty(et.Id))
                        {
                            eptElement.SetAttributeValue("id", et.Id);
                        }

                        foreach (var attr in tag.Other.OfType<XAttribute>())
                        {
                            eptElement.SetAttributeValue(attr.Name, attr.Value);
                        }

                        if (!string.IsNullOrEmpty(et.Value))
                        {
                            eptElement.Add(et.Value);
                        }

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
                    {
                        phElement.Add(tag.Value);
                    }

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
                    var startIndex = parts.IndexOf(anno);
                    var endIndex = parts.IndexOf(anno.EndAnnotationReference);
                    if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
                    {
                        for (var i = startIndex + 1; i < endIndex; i++)
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
        for (var i = 0; i < input.Length; i++)
        {
            var cp = char.ConvertToUtf32(input, i);
            var valid =
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
                    //fileTransformation.SkeletonOther = new List<XElement> { internalFile };
                }

                var externalFile = skeleton.Element(XliffNs + "external-file");
                if (externalFile != null)
                {
                    fileTransformation.OriginalReference = externalFile.Get("href");
                    fileTransformation.SkeletonOther = new List<XElement> { externalFile };
                }
            }

            fileTransformation.Notes = DeserializeNotes(header.Elements(XliffNs + "note"));
            foreach (var node in header.Elements().Where(x => x.Name.LocalName != "note" && x.Name.LocalName != "skl"))
            {
                var cleanedNode = node.FixTabulationWhitespace();
                fileTransformation.Other.Add(cleanedNode);
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
        return elements.Select(note => new Note(note.Value)
            {
                Id = note.Get("id"),
                Priority = note.GetInt("priority"),
                Category = note.Get(BlackbirdNs + "category"),
                Reference = note.Get(BlackbirdNs + "reference"),
                LanguageTarget = note.GetLanguageTarget(BlackbirdNs + "languageTarget"),
                Other = note.Attributes().GetRemaining(["id", "priority", "category", "reference"]).Where(a => a.Name.Namespace != BlackbirdNs && a.Name.Namespace != XliffNs).ToList()
            })
            .ToList();
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
                    Name = element.Get("resname"),
                    CanResegment = element.GetBool(BlackbirdNs + "canResegment"),
                    Translate = element.GetBool("translate"),
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
                
                var otherElements = element.Elements().Where(e => e.Name.LocalName != "note" && e.Name.LocalName != "group" && e.Name.LocalName != "trans-unit").ToList();
                group.Other.AddRange(otherElements.Select(x => x.FixTabulationWhitespace()));
                ProcessBodyContent(element, group);

                if (parent is Group parentGroup)
                {
                    parentGroup.Children.Add(group);
                }
                else if (parent is Transformation transformation)
                {
                    transformation.Children.Add(group);
                }
            }
            else if (element.Name == XliffNs + "trans-unit")
            {
                var unit = new Unit
                {
                    Id = element.Get("id"),
                    Name = element.Get("resname"),
                    CanResegment = element.GetBool(BlackbirdNs + "canResegment"),
                    Translate = element.GetBool("translate"),
                    SourceDirection = element.GetDirection(BlackbirdNs + "srcDir"),
                    TargetDirection = element.GetDirection(BlackbirdNs + "trgDir"),
                };
                
                var hasSegSource = element.Element(XliffNs + "seg-source") != null;
                if (hasSegSource)
                {
                    unit.Other.Add(new XAttribute(HasSegSourceAttrName, "true"));
                }
                
                var other = element.Attributes().Where(a => a.Name.Namespace != BlackbirdNs && a.Name.Namespace != XliffNs).ToList();
                unit.Other.AddRange(other.GetRemaining(["id", "resname", "canResegment", "translate", "srcDir", "trgDir"]));
                
                var otherElements = element.Elements().Where(e => e.Name.LocalName != "source" && e.Name.LocalName != "target" && e.Name.LocalName != "seg-source" && e.Name.LocalName != "mrk" && e.Name.LocalName != "note").ToList();
                unit.Other.AddRange(otherElements.Select(x => x.FixTabulationWhitespace()));

                var source = element.Element(XliffNs + "source");
                var target = element.Element(XliffNs + "target");
                var segSource = element.Element(XliffNs + "seg-source");
                var mediaType = element.Get(BlackbirdNs + "tagHandling");
                
                var targetState = target?.Get("state");
                bool? segmentIgnorable = null;
                if (element.Get("translate") == "yes")
                {
                    segmentIgnorable = false;
                }
                else if (element.Get("translate") == "no")
                {
                    segmentIgnorable = true;
                }

                if (segSource != null)
                {
                    foreach (var mrkElement in segSource.Elements(XliffNs + "mrk").Where(m => m.Get("mtype") == "seg"))
                    {
                        var segment = new Segment
                        {
                            Id = mrkElement.Get("mid"),
                            Source = ExtractTextParts(mrkElement),
                            OriginalMediaType = mediaType,
                            Ignorable = segmentIgnorable
                        };

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
                                segment.Ignorable = matchingTargetMrk.GetBool(BlackbirdNs + "ignorable") ?? segmentIgnorable;
                                
                                var mrkState = matchingTargetMrk.Get(BlackbirdNs + "customState");
                                if (!string.IsNullOrEmpty(mrkState))
                                {
                                    var target12State = mrkState.ToTarget12State();
                                    if (target12State.HasValue)
                                    {
                                        segment.State = target12State.Value.ToSegmentState();
                                    }
                                }
                                else if (!string.IsNullOrEmpty(targetState))
                                {
                                    var target12State = targetState.ToTarget12State();
                                    if (target12State.HasValue)
                                    {
                                        segment.State = target12State.Value.ToSegmentState();
                                    }
                                }
                                
                                segment.TargetAttributes = matchingTargetMrk.Attributes()
                                    .GetRemaining(["mtype", "mid", "phase-name", "canResegment", "ignorable", "order", "state"])
                                    .Where(x => x.Name.Namespace != BlackbirdNs)
                                    .ToList();
                            }
                        }
                        
                        unit.Segments.Add(segment);
                    }
                }
                else
                {
                    var segment = new Segment
                    {
                        Id = element.Get("id"),
                        Source = source != null ? ExtractTextParts(source) : new List<TextPart>(),
                        Target = target != null ? ExtractTextParts(target) : new List<TextPart>(),
                        OriginalMediaType = mediaType,
                        SourceAttributes = source?.Attributes().ToList() ?? new List<XAttribute>(),
                        TargetAttributes = target?.Attributes().GetRemaining(["state"]).ToList() ?? new List<XAttribute>(),
                        Ignorable = segmentIgnorable
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

                    if (target != null)
                    {
                        var stateAttr = target.Get("state");
                        if (!string.IsNullOrEmpty(stateAttr))
                        {
                            var target12State = stateAttr.ToTarget12State();
                            if (target12State.HasValue)
                            {
                                segment.State = target12State.Value.ToSegmentState();
                            }
                        }
                    }

                    if (element.Get("phase-name") != null)
                    {
                        segment.SubState = element.Get("phase-name");
                    }

                    unit.Segments.Add(segment);
                }

                unit.Notes = DeserializeNotes(element.Elements(XliffNs + "note"));
                if (parent is Group parentGroup)
                {
                    parentGroup.Children.Add(unit);
                }
                else if (parent is Transformation transformation)
                {
                    transformation.Children.Add(unit);
                }
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
                    if (mtype != "seg")
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
                else if(!string.IsNullOrEmpty(childElement.Value))
                {
                    parts.Add(new TextPart { Value = childElement.Value });
                }
            }
        }

        return parts;
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