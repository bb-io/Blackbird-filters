using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff;
using Blackbird.Filters.Xliff.Xliff2;
using System.Text;

namespace Blackbird.Filters.Tests.Html;

[TestFixture]
public class HtmlCoderTests : TestBase
{
    private (string, CodedContent, string) ProcessSource(string file)
    {
        var html = File.ReadAllText(file, Encoding.UTF8);
        var content = HtmlContentCoder.Deserialize(html);
        var transformation = content.CreateTransformation("en", "nl");
        var serialized = Xliff2Serializer.Serialize(transformation);
        var deserialized = Xliff2Serializer.Deserialize(serialized);
        var returned = HtmlContentCoder.Serialize(deserialized!.Source());
        DisplayXml(serialized);
        Console.WriteLine("------");
        DisplayHtml(returned);

        return (html, content, returned);
    }

    private (string, CodedContent, string) ProcessTarget(string file)
    {
        var html = File.ReadAllText(file, Encoding.UTF8);
        var content = HtmlContentCoder.Deserialize(html);
        var transformation = content.CreateTransformation("en", "nl");
        var serialized = Xliff2Serializer.Serialize(transformation);
        serialized = PseudoTranslateXliff(serialized);
        var deserialized = Xliff2Serializer.Deserialize(serialized);
        var returned = HtmlContentCoder.Serialize(deserialized!.Target());
        DisplayXml(serialized);
        Console.WriteLine("------");
        DisplayHtml(returned);

        return (html, content, returned);
    }

    public string TranslateFile(string fileContent)
    {
        // File content can be either HTML or XLIFF (more formats to follow soon)
        var transformation = Transformation.TryParse(fileContent);

        foreach(var segment in transformation.GetSegments()) // You can also add .batch() to batch segments
        {
            // Implement API calls here
            segment.SetTarget(segment.GetSource() + " - Translated!"); 

            // More state manipulations can be performed here
            segment.State = SegmentState.Translated; 
        }
        return HtmlContentCoder.Serialize(transformation.Target());
    }

    private string PseudoTranslateXliff(string xliff)
    {
        var transformation = Xliff2Serializer.Deserialize(xliff);
        foreach (var segment in transformation.GetSegments())
        {
            segment.SetTarget(segment.GetSource() + "TRANSLATED");
            segment.State = SegmentState.Translated;
        }
        return Xliff2Serializer.Serialize(transformation);
    }

    [Test]
    public void Is_html()
    {
        // Arrange
        var html = "<html><head></head><body></body></html>";
        var notHtml = "<xml><root>Test</root></xml>";
        var notHtml2 = "Plain string";

        // Act
        var result = HtmlContentCoder.IsHtml(html);
        var result2 = HtmlContentCoder.IsHtml(notHtml);
        var result3 = HtmlContentCoder.IsHtml(notHtml2);

        // Assert
        Assert.IsTrue(result);
        Assert.IsFalse(result2);
        Assert.IsFalse(result3);
    }

    [Test]
    public void Empty()
    {
        var (html, content, returned) = ProcessSource("Html/Files/empty.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Simple()
    {
        var (html, content, returned) = ProcessSource("Html/Files/simple.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
    }

    [Test]
    public void With_br()
    {
        var (html, content, returned) = ProcessSource("Html/Files/with_br.html");
        
        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Inline_tags()
    {
        var (html, content, returned) = ProcessSource("Html/Files/inline_tags.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Floating_text()
    {
        var (html, content, returned) = ProcessSource("Html/Files/floating_text.html");
      
        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
    }

    [Test]
    public void Img_alt_text()
    {
        var (html, content, returned) = ProcessSource("Html/Files/img_alt_text.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
        Assert.IsTrue(content.TextUnits.Any(x => x.GetCodedText().Contains("Girl with a jacket", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Button_subflows()
    {
        var (html, content, returned) = ProcessSource("Html/Files/subflows.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
        Assert.IsTrue(content.TextUnits.Any(x => x.GetCodedText().Contains("Click here to start!", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Img_title_attr()
    {
        var (html, content, returned) = ProcessSource("Html/Files/img_title_attr.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsTrue(content.TextUnits.Any(x => x.GetCodedText().Contains("I'm a tooltip", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Comments()
    {
        var (html, content, returned) = ProcessSource("Html/Files/comments.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Script()
    {
        var (html, content, returned) = ProcessSource("Html/Files/script.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsFalse(content.TextUnits.Any(x => x.GetCodedText().Contains("function", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Meta_content()
    {
        var (html, content, returned) = ProcessSource("Html/Files/meta_content.html");

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsTrue(content.TextUnits.Any(x => x.GetCodedText().Contains("Free Web tutorials", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Full_example()
    {
        var (html, content, returned) = ProcessSource("Html/Files/contentful.html");

        HtmlAssert.AreEqual(html, returned);
    }

    [Test]
    public void Full_translated()
    {
        var (html, content, returned) = ProcessTarget("Html/Files/contentful.html");

        HtmlAssert.AreNotEqual(html, returned);
    }

}
