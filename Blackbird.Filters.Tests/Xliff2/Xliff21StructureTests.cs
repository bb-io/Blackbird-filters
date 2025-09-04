using Blackbird.Filters.Enums;
using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff2;
using System.Text;
using System.Xml;

namespace Blackbird.Filters.Tests.Xliff2;

[TestFixture]
public class Xliff21StructureTests : TestBase
{
    [TestCase("toplevelmeta_multi")]
    [TestCase("toplevelmeta_single")]
    public void Top_Level(string fileName)
    {
        var xliff = File.ReadAllText($"Xliff2/Files/{fileName}.xliff", Encoding.UTF8);
        Console.WriteLine("Original:");
        DisplayXml(xliff);

        var originalXliff = Transformation.Parse(xliff, $"{fileName}.xliff");    
        var xliff21 = Xliff2Serializer.Serialize(originalXliff, Xliff2Version.Xliff21);

        var doc = XmlAssert.NormalizedDocument(xliff21);
        var root = doc.DocumentElement;

        Assert.That(root?.LocalName, Is.EqualTo("xliff"));
        Assert.That(
            root.ChildNodes.Cast<XmlNode>().All(n =>
                n.NodeType == XmlNodeType.Element && n.LocalName == "file"),
            Is.True,
            "Root contains non-<file> children."
        );

        Console.WriteLine("\r\n2.1:");
        DisplayXml(xliff21);

        var secondTransformation = Transformation.Parse(xliff21, $"{fileName}.xliff");
        var finalXliff = secondTransformation.Serialize();
        Console.WriteLine("\r\nFinal:");
        DisplayXml(finalXliff);

        XmlAssert.AreEqual(xliff, finalXliff);
        
    }
}