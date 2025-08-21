using Org.XmlUnit.Builder;
using Org.XmlUnit.Diff;
using System.Xml;
using System.Xml.Linq;

namespace Blackbird.Filters.Tests.CustomAssertions;
public static class XmlAssert
{
    private static void NormalizeCDataInNode(XmlNode node)
    {
        if (node == null) return;

        var childNodes = node.ChildNodes;
        for (int i = 0; i < childNodes.Count; i++)
        {
            XmlNode child = childNodes[i];

            // If it's a CDATA section, replace it with a regular text node
            if (child.NodeType == XmlNodeType.CDATA)
            {
                XmlText newText = node.OwnerDocument.CreateTextNode(child.Value);
                node.ReplaceChild(newText, child);
            }
            else if (child.HasChildNodes)
            {
                NormalizeCDataInNode(child); // Recursive call
            }
        }
    }

    private static void RemoveEmptyTargets(XmlNode node)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            foreach (XmlNode child in node.ChildNodes.Cast<XmlNode>().ToList())
            {
                RemoveEmptyTargets(child);
            }

            if (node.Name == "target" &&
                !node.HasChildNodes &&
                !(node as XmlElement)?.HasAttributes == true)
            {
                node.ParentNode?.RemoveChild(node);
            }
        }
    }

    public static void AreEqual(string expectedXml, string actualXml, string? message = null)
    {
        static string Normalize(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            RemoveEmptyTargets(doc.DocumentElement);
            NormalizeCDataInNode(doc);

            return doc.OuterXml;
        }

        string normalizedExpected = Normalize(expectedXml);
        string normalizedActual = Normalize(actualXml);

        var diff = DiffBuilder.Compare(normalizedExpected)
                              .WithTest(normalizedActual)
                              .IgnoreWhitespace()
                              .NormalizeWhitespace()
                              .WithNodeMatcher(new DefaultNodeMatcher(ElementSelectors.ByNameAndText))
                              .WithAttributeFilter(attr =>
                                  !(attr.Name == "xml:space" && attr.Value == "default")
                                  && !((attr.OwnerElement?.Name == "file") && attr.Name == "id")
                              )
                              .CheckForSimilar()
                              .Build();

        if (diff.HasDifferences())
        {
            Assert.Fail(message ?? $"XML documents are not equal:\n{diff}");
        }
    }

    public static void AreEqual(XDocument expected, XDocument actual, string? message = null)
    {
        AreEqual(expected.ToString(), actual.ToString(), message);
    }
}
