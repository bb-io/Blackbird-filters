using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff1;
using System.Text;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Tests.Html;
using Blackbird.Filters.Xliff.Xliff2;
using System.Xml.Linq;
using Blackbird.Filters.Transformations;

namespace Blackbird.Filters.Tests.Xliff1;

[TestFixture]
public class Xliff12ValidTestSuiteTests : TestBase
{
    private static readonly XNamespace BlackbirdNs = "http://blackbird.io/";
    private static readonly XNamespace XliffNs = "urn:oasis:names:tc:xliff:document:1.2";
    
    [Test]
    public void Basic()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/basic.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        Assert.That(content.Children.Count(x => x is Transformation), Is.EqualTo(0));

        // Additional assertions to verify structure
        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Id, Is.EqualTo("f1"));
        
        var units = content.GetUnits().ToList();
        Assert.That(units.Count, Is.EqualTo(1));
        Assert.That(units[0].Name, Is.EqualTo("greeting"));
        
        var segments = units[0].Segments;
        Assert.That(segments.Count, Is.EqualTo(1));
        
        var segment = segments[0];
        Assert.That(segment.Source[0].Value, Is.EqualTo("Hello World"));
        Assert.That(segment.Target[0].Value, Is.EqualTo("Bonjour le monde"));
        
        // Verify notes were preserved
        Assert.That(content.Notes.Count, Is.EqualTo(1));
        Assert.That(content.Notes[0].Text, Is.EqualTo("Sample XLIFF 1.2 file"));
        Assert.That(units[0].Notes.Count, Is.EqualTo(1));
        Assert.That(units[0].Notes[0].Text, Is.EqualTo("Simple greeting"));
    }

    [Test]
    public void Segmented()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/segmented.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
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

    [Test]
    public void InitialSegments()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/emptyTarget.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify structure
        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Id, Is.EqualTo("f1"));
        
        var units = content.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.IsInitial)));
    }
    
    [Test]
    public void TranslatedSegments()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/translate.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify structure
        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Id, Is.EqualTo("f1"));
        
        var units = content.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.Ignorable.HasValue && y.Ignorable.Value)));
    }
    
    [Test]
    public void ApprovedSegments()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/approved.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Id, Is.EqualTo("f1"));
        
        var units = content.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.State == null)));
    }
    
    [Test]
    public void MultipleFiles()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/multifile.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        Assert.That(content.Children.Count, Is.EqualTo(2));
    }
    
    [Test]
    public void Complex()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/complex.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void EverythingCore()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/everythingCore.xliff", Encoding.UTF8);
        
        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);
        
        // Assert
        XmlAssert.AreEqual(xliff, returned);
    }
    
    [Test]
    public void ContentfulTo12()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff2/Files/contentful.xliff", Encoding.UTF8);

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);
    }
    
    [Test]
    public void StateHandling()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/state.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify state handling
        Assert.That(content, Is.Not.Null);
        
        var segments = content!.GetSegments().ToList();
        Assert.That(segments.Count, Is.EqualTo(1));
        
        var segment = segments[0];
        
        // Check if the state is correctly set to Translated
        Assert.That(segment.State, Is.EqualTo(SegmentState.Translated));
        
        // Verify the state is properly written back in serialization
        Assert.That(returned.Contains("state=\"translated\""), Is.True);
    }
    
    [Test]
    public void StateHandlingWithSegmentation()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/stateWithSegmentation.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify state handling with segmentation
        Assert.That(content, Is.Not.Null);
        
        var segments = content!.GetSegments().ToList();
        Assert.That(segments.Count, Is.EqualTo(2));
        
        // Check if both segments have the correct state
        Assert.That(segments.All(s => s.State == SegmentState.Final || s.State == SegmentState.Translated), Is.True);
    }
    
    [Test]
    public void DifferentStatesHandlingWithSegmentation()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/differentStatesWithSegmentation.xliff", Encoding.UTF8);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify state handling with segmentation
        Assert.That(content, Is.Not.Null);
        
        var segments = content!.GetSegments().ToList();
        Assert.That(segments.Count, Is.EqualTo(2));
        
        // Check if both segments have the correct state
        Assert.That(segments.All(s => s.State == SegmentState.Final || s.State == SegmentState.Translated), Is.True);
    }
}