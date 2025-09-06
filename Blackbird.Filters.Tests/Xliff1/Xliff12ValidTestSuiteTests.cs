using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff1;
using System.Text;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Xliff.Xliff2;
using System.Xml.Linq;
using Blackbird.Filters.Transformations;

namespace Blackbird.Filters.Tests.Xliff1;

[TestFixture]
public class Xliff12ValidTestSuiteTests : TestBase
{
    private static readonly XNamespace BlackbirdNs = "http://blackbird.io/";
    private static readonly XNamespace XliffNs = "urn:oasis:names:tc:xliff:document:1.2";

    private (string content, string fileName) ReadXliffFile(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        var content = File.ReadAllText(relativePath, Encoding.UTF8);
        return (content, fileName);
    }

    private Transformation PerformDeserializeSerializeTest(string relativePath)
    {
        // Arrange
        var (xliff, fileName) = ReadXliffFile(relativePath);

        // Act
        var content = Xliff1Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);

        // Assert
        XmlAssert.AreEqual(xliff, returned);
        
        return content;
    }
    
    [Test]
    public void Basic()
    {
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/basic.xliff");
        
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

    [TestCase]
    public void Basic_IsXliff1()
    {
        // Arrange & Act
        var filePath = $"Xliff1/Files/basic.xliff";
        var fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        // Act
        var result = Xliff1Serializer.IsXliff1(fileContent);

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("<html version=\"4.01\"></html>")]
    [TestCase("<xliff version=\"2.1\"></xliff>")]
    public void Basic_IsXliff1_ForInvalidContent(string fileContent)
    {
        // Act
        var result = Xliff1Serializer.IsXliff1(fileContent);

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase]
    public void Basic_GetVersion()
    {
        // Arrange & Act
        var filePath = $"Xliff1/Files/basic.xliff";
        var fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        // Act
        var result = Xliff1Serializer.TryGetXliffVersion(fileContent, out var version);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(version, Is.EqualTo("1.2"));
        });
    }

    [Test]
    public void Segmented()
    {
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/segmented.xliff");
        
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
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/emptyTarget.xliff");
        
        // Additional assertions to verify structure
        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Id, Is.EqualTo("f1"));
        
        var units = content.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.IsInitial)));
    }
    
    [Test]
    public void TranslatedSegments()
    {
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/translate.xliff");
        
        // Additional assertions to verify structure
        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Id, Is.EqualTo("f1"));
        
        var units = content.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.Ignorable.HasValue && y.Ignorable.Value)));
    }
    
    [Test]
    public void ApprovedSegments()
    {
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/approved.xliff");
        
        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Id, Is.EqualTo("f1"));
        
        var units = content.GetUnits().ToList();
        Assert.True(units.All(x => x.Segments.All(y => y.State == null)));
    }
    
    [Test]
    public void MultipleFiles()
    {
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/multifile.xliff");
        
        Assert.That(content.Children.Count, Is.EqualTo(2));
    }
    
    [Test]
    public void Complex()
    {
        // Act & Assert
        PerformDeserializeSerializeTest("Xliff1/Files/complex.xliff");
    }

    [Test]
    public void EverythingCore()
    {
        // Act & Assert
        PerformDeserializeSerializeTest("Xliff1/Files/everythingCore.xliff");
    }
    
    [Test]
    public void ContentfulTo12()
    {
        // Arrange
        var (xliff, fileName) = ReadXliffFile("Xliff2/Files/contentful.xliff");

        // Act
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff1Serializer.Serialize(content);
        DisplayXml(returned);
    }
    
    [Test]
    public void StateHandling()
    {
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/state.xliff");
        
        // Additional assertions to verify state handling
        Assert.That(content, Is.Not.Null);
        
        var segments = content!.GetSegments().ToList();
        Assert.That(segments.Count, Is.EqualTo(1));
        
        var segment = segments[0];
        
        // Check if the state is correctly set to Translated
        Assert.That(segment.State, Is.EqualTo(SegmentState.Translated));
    }
    
    [Test]
    public void StateHandlingWithSegmentation()
    {
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/stateWithSegmentation.xliff");
        
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
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/differentStatesWithSegmentation.xliff");
        
        // Additional assertions to verify state handling with segmentation
        Assert.That(content, Is.Not.Null);
        
        var segments = content!.GetSegments().ToList();
        Assert.That(segments.Count, Is.EqualTo(2));
        
        // Check if both segments have the correct state
        Assert.That(segments.All(s => s.State == SegmentState.Final || s.State == SegmentState.Translated), Is.True);
    }
    
    [Test]
    public void MxliffFileTest()
    {
        // Act & Assert
        var content = PerformDeserializeSerializeTest("Xliff1/Files/Mr.Coffee Mug Warmer_en-US.html-en-nl-T.mxliff");
        
        // Additional assertions to verify state handling with segmentation
        Assert.That(content, Is.Not.Null);
    }
}