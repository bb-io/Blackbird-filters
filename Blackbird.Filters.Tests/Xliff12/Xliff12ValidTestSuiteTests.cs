using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff12;
using System.Text;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Tests.Html;
using Blackbird.Filters.Xliff.Xliff2;
using System.Xml.Linq;

namespace Blackbird.Filters.Tests.Xliff12;

[TestFixture]
public class Xliff12ValidTestSuiteTests : HtmlTestBase
{
    private static readonly XNamespace BlackbirdNs = "http://blackbird.io/";
    private static readonly XNamespace XliffNs = "urn:oasis:names:tc:xliff:document:1.2";
    
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
        var file = content.Children[0] as Transformations.Transformation;
        Assert.That(file, Is.Not.Null);
        Assert.That(file!.Id, Is.EqualTo("f1"));
        
        var units = file.GetUnits().ToList();
        Assert.That(units.Count, Is.EqualTo(1));
        Assert.That(units[0].Name, Is.EqualTo("greeting"));
        Assert.That(units[0].Segments[0].IsInitial, Is.False);
        
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

    [Test]
    public void InitialSegments()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/emptyTarget.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify structure
        var file = content.Children[0] as Transformations.Transformation;
        Assert.That(file, Is.Not.Null);
        Assert.That(file!.Id, Is.EqualTo("f1"));
        
        var units = file.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.IsInitial)));
    }
    
    [Test]
    public void TranslatedSegments()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/translate.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify structure
        var file = content.Children[0] as Transformations.Transformation;
        Assert.That(file, Is.Not.Null);
        Assert.That(file!.Id, Is.EqualTo("f1"));
        
        var units = file.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.Ignorable.HasValue && y.Ignorable.Value)));
    }
    
    [Test]
    public void ApprovedSegments()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/approved.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify structure
        var file = content.Children[0] as Transformations.Transformation;
        Assert.That(file, Is.Not.Null);
        Assert.That(file!.Id, Is.EqualTo("f1"));
        
        var units = file.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.State == SegmentState.Final)));
    }
    
    [Test]
    public void MultipleFiles()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/multifile.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
    }
    
    [Test]
    public void Complex()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/complex.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
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
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);
    }
    
    [Test]
    public void StateHandling()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/state.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify state handling
        var file = content.Children[0] as Transformations.Transformation;
        Assert.That(file, Is.Not.Null);
        
        var segments = file!.GetSegments().ToList();
        Assert.That(segments.Count, Is.EqualTo(1));
        
        var segment = segments[0];
        
        // Check if the state is correctly set to Translated
        Assert.That(segment.State, Is.EqualTo(SegmentState.Translated));
        
        // Check if the original state is preserved as a custom attribute
        var customStateAttr = segment.TargetAttributes
            .FirstOrDefault(a => a.Name == BlackbirdNs + "customState");
        Assert.That(customStateAttr, Is.Not.Null);
        Assert.That(customStateAttr?.Value, Is.EqualTo("translated"));
        
        // Verify the state is properly written back in serialization
        Assert.That(returned.Contains("state=\"translated\""), Is.True);
    }
    
    [Test]
    public void StateHandlingWithSegmentation()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/stateWithSegmentation.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify state handling with segmentation
        var file = content.Children[0] as Transformations.Transformation;
        Assert.That(file, Is.Not.Null);
        
        var segments = file!.GetSegments().ToList();
        Assert.That(segments.Count, Is.EqualTo(2));
        
        // Check if both segments have the correct state
        Assert.That(segments.All(s => s.State == SegmentState.Final), Is.True);
        
        // Check if the original state is preserved as a custom attribute for both segments
        foreach (var segment in segments)
        {
            var customStateAttr = segment.TargetAttributes
                .FirstOrDefault(a => a.Name == BlackbirdNs + "customState");
            Assert.That(customStateAttr, Is.Not.Null);
            Assert.That(customStateAttr?.Value, Is.EqualTo("final"));
        }
        
        // Verify the states are properly written back in serialization
        var doc = XDocument.Parse(returned);
        var stateCount = doc.Descendants(XName.Get("mrk", XliffNs.NamespaceName))
            .Where(e => e.Attribute("state")?.Value == "final")
            .Count();
        Assert.That(stateCount, Is.EqualTo(2));
    }
    
    [Test]
    public void DifferentStatesHandlingWithSegmentation()
    {
        // Arrange
        var xliff = File.ReadAllText("Xliff12/Files/differentStatesWithSegmentation.xliff", Encoding.UTF8);

        // Act
        var content = Xliff12Serializer.Deserialize(xliff);
        var returned = Xliff12Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        // Additional assertions to verify state handling with segmentation
        var file = content.Children[0] as Transformations.Transformation;
        Assert.That(file, Is.Not.Null);
        
        var segments = file!.GetSegments().ToList();
        Assert.That(segments.Count, Is.EqualTo(2));
        
        // Check if both segments have the correct state
        Assert.That(segments.All(s => s.State == SegmentState.Final || s.State == SegmentState.Translated), Is.True);
        
        foreach (var segment in segments)
        {
            var customStateAttr = segment.TargetAttributes
                .FirstOrDefault(a => a.Name == BlackbirdNs + "customState");
            Assert.That(customStateAttr, Is.Not.Null);
        }
    }
}