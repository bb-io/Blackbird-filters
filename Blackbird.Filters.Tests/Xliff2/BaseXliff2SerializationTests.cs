using Blackbird.Filters.Coders;
using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        Assert.That(content.Transformations.Count(), Is.EqualTo(0));
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
        Assert.That(content.Transformations.Count(), Is.EqualTo(1));        

        // Additional assertions to verify structure
        var file = content.Transformations[0];
        Assert.That(file.GetUnits().Count(), Is.EqualTo(1));
        Assert.That(file.Id, Is.EqualTo("f1"));
        Assert.That(file.GetUnits().FirstOrDefault()!.Notes.Count, Is.EqualTo(1));
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
}
