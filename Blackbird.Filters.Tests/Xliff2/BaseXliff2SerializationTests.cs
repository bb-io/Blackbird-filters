using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff2;
using System.Text;
using System.Xml.Linq;

namespace Blackbird.Filters.Tests.Xliff2;

[TestFixture]
public class BaseXliff2SerializationTests : TestBase
{
    [Test]
    public void Empty()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/empty.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        Display(content);

        // Assert
        Assert.That(content.Children.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Basic()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/basic.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        Assert.That(content.Children.Count(), Is.EqualTo(1));        

        // Additional assertions to verify structure
        Assert.That(content.GetUnits().Count(), Is.EqualTo(1));
        Assert.That(content.GetUnits().FirstOrDefault()!.Notes.Count, Is.EqualTo(1));
    }

    [TestCase]
    public void Basic_IsXliff2()
    {
        // Arrange & Act
        var filePath = $"Xliff2/Files/basic.xliff";
        var fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        // Act
        var result = Xliff2Serializer.IsXliff2(fileContent);

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("<html version=\"4.01\"></html>")]
    [TestCase("<xliff version=\"1.2\"></xliff>")]
    public void Basic_IsXliff2_ForInvalidContent(string fileContent)
    {
        // Act
        var result = Xliff2Serializer.IsXliff2(fileContent);

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase]
    public void Basic_GetVersion()
    {
        // Arrange & Act
        var filePath = $"Xliff2/Files/basic.xliff";
        var fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        // Act
        var result = Xliff2Serializer.TryGetXliffVersion(fileContent, out var version);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(version, Is.EqualTo("2.2"));
        });
    }

    [Test]
    public void Complex()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/complex.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void Contentful()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/contentful.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        Display(content);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        Assert.That(content.GetSegments().All(x => !x.GetSource().Contains("\n")), "Unit contains newlines");
        Assert.That(content.GetSegments().All(x => x.GetSource()[0] != ' '), "First character is a space");
    }

    [Test]
    public void Groups()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/groups.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void Custom_xml()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/custom-xml.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void Annotations()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/annotations.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
    }


    [Test]
    public void Multifile()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/multifile.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void OriginalDataBeforeSegments()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/originalDataBeforeSegments.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        var doc = XDocument.Parse(returned);
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.2";
        var units = doc.Descendants(ns + "unit");
        foreach (var unit in units)
        {
            var children = unit.Elements().ToList();
            var originalDataIndex = children.FindIndex(e => e.Name.LocalName == "originalData");
            var segmentIndex = children.FindIndex(e => e.Name.LocalName == "segment");
            Assert.That(originalDataIndex, Is.GreaterThanOrEqualTo(0), "originalData tag not found in unit");
            Assert.That(segmentIndex, Is.GreaterThanOrEqualTo(0), "segment tag not found in unit");
            Assert.That(originalDataIndex, Is.LessThan(segmentIndex), "originalData does not precede segment in unit");
        }
    }
}