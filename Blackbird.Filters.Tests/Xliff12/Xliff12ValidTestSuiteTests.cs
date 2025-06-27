using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff12;
using System.Text;

namespace Blackbird.Filters.Tests.Xliff12;

[TestFixture]
public class Xliff12ValidTestSuiteTests : TestBase
{
    [Test]
    public void Basic()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/basic.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        Assert.That(content.Children.Count, Is.EqualTo(1));

        // Additional assertions to verify structure
        var file = content.Children[0] as Blackbird.Filters.Transformations.Transformation;
        Assert.That(file, Is.Not.Null);
        Assert.That(file!.Id, Is.EqualTo("f1"));
        
        var units = file.GetUnits().ToList();
        Assert.That(units.Count, Is.EqualTo(1));
        Assert.That(units[0].Name, Is.EqualTo("greeting"));
        
        var segments = units[0].Segments;
        Assert.That(segments.Count, Is.EqualTo(1));
        
        var segment = segments[0];
        Assert.That(segment.Source[0].Value, Is.EqualTo("Hello World"));
        Assert.That(segment.Target[0].Value, Is.EqualTo("Bonjour le monde"));
        
        // Verify notes were preserved
        Assert.That(file.Notes.Count, Is.EqualTo(1));
        Assert.That(file.Notes[0].Text, Is.EqualTo("Sample XLIFF 1.2 file"));
        Assert.That(units[0].Notes.Count, Is.EqualTo(1));
        Assert.That(units[0].Notes[0].Text, Is.EqualTo("Simple greeting"));
    }

    [Test]
    public void Segmented()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/segmented.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Verify segmentation was preserved
        var units = content.GetUnits().ToList();
        Assert.That(units.Count, Is.EqualTo(1));
        
        var segments = units[0].Segments;
        Assert.That(segments.Count, Is.EqualTo(2));
        
        Assert.That(segments[0].Source[0].Value, Is.EqualTo("First sentence."));
        Assert.That(segments[0].Target[0].Value, Is.EqualTo("Première phrase."));
        
        Assert.That(segments[1].Source[0].Value, Is.EqualTo("Second sentence."));
        Assert.That(segments[1].Target[0].Value, Is.EqualTo("Deuxième phrase."));
    }
}
