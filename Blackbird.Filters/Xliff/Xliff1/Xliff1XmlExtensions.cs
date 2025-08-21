using System.Text;
using System.Xml.Linq;

namespace Blackbird.Filters.Xliff.Xliff1;

public static class Xliff1XmlExtensions
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
    
    public static string CompactTextElements(string xmlString)
    {
        var lines = xmlString.Split('\n');
        var result = new List<string>();
        var inTextElement = false;
        var textContent = new StringBuilder();
        var textIndent = string.Empty;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("<source") || trimmedLine.StartsWith("<target"))
            {
                inTextElement = true;
                textContent.Clear();
                textContent.Append(trimmedLine);

                textIndent = line.Substring(0, line.IndexOf('<'));
                if (trimmedLine.EndsWith("</source>") || trimmedLine.EndsWith("</target>") || trimmedLine.EndsWith("/>"))
                {
                    result.Add(line);
                    inTextElement = false;
                }
            }
            else if (inTextElement)
            {
                if (trimmedLine.EndsWith("</source>") || trimmedLine.EndsWith("</target>"))
                {
                    textContent.Append(trimmedLine);
                    result.Add(textIndent + textContent);
                    inTextElement = false;
                }
                else
                {
                    textContent.Append(trimmedLine);
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