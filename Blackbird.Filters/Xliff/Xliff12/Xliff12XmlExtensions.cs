using System.Text;
using System.Xml.Linq;

namespace Blackbird.Filters.Xliff.Xliff12;

public static class Xliff12XmlExtensions
{
    public static XElement FixTabulationWhitespace(this XElement element)
    {
        var newElement = new XElement(element.Name);
        foreach (var attr in element.Attributes())
        {
            newElement.SetAttributeValue(attr.Name, attr.Value);
        }
        
        foreach (var node in element.Nodes())
        {
            if (node is XElement childElement)
            {
                newElement.Add(FixTabulationWhitespace(childElement));
            }
            else if (node is XText textNode && !string.IsNullOrWhiteSpace(textNode.Value))
            {
                newElement.Add(new XText(textNode.Value));
            }
        }
        
        return newElement;
    }
    
    public static string CompactSourceElements(string xmlString)
    {
        var lines = xmlString.Split('\n');
        var result = new List<string>();
        var inSourceElement = false;
        var sourceContent = new StringBuilder();
        var sourceIndent = string.Empty;

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
    
    internal static XElement? GetRootNode(string content)
    {
        try
        {
            var doc = XDocument.Parse(content);
            return doc.Root;
        }
        catch (Exception)
        {
            var byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (content.StartsWith(byteOrderMarkUtf8))
            {
                content = content.Remove(0, byteOrderMarkUtf8.Length);
            }

            var doc = XDocument.Parse(content);
            return doc.Root;
        }
    }
}