using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Transformations;
using System.Text;

namespace Blackbird.Filters.Tests.Html;

[TestFixture]
public class HtmlCoderTests : TestBase
{
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
        var result = Process("Html/Files/empty.html");


        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Simple()
    {
        var result = Process("Html/Files/simple.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(3));
    }

    [Test]
    public void With_br()
    {
        var result = Process("Html/Files/with_br.html");
        
        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Inline_tags()
    {
        var result = Process("Html/Files/inline_tags.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Floating_text()
    {
        var result = Process("Html/Files/floating_text.html");
      
        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(3));
    }

    [Test]
    public void Img_alt_text()
    {
        var result = Process("Html/Files/img_alt_text.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(3));
        Assert.IsTrue(result.Source.TextUnits.Any(x => x.GetCodedText().Contains("Girl with a jacket", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Button_subflows()
    {
        var result = Process("Html/Files/subflows.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(3));
        Assert.IsTrue(result.Source.TextUnits.Any(x => x.GetCodedText().Contains("Click here to start!", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Img_title_attr()
    {
        var result = Process("Html/Files/img_title_attr.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsTrue(result.Source.TextUnits.Any(x => x.GetCodedText().Contains("I'm a tooltip", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Comments()
    {
        var result = Process("Html/Files/comments.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Script()
    {
        var result = Process("Html/Files/script.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsFalse(result.Source.TextUnits.Any(x => x.GetCodedText().Contains("function", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Meta_content()
    {
        var result = Process("Html/Files/meta_content.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsTrue(result.Source.TextUnits.Any(x => x.GetCodedText().Contains("Free Web tutorials", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void Full_example()
    {
        var result = Process("Html/Files/contentful.html");

        HtmlAssert.AreEqual(result.Original, result.SourceString);
    }

    [Test]
    public void Full_translated()
    {
        var result = Process("Html/Files/contentful.html");

        HtmlAssert.AreNotEqual(result.Original, result.TargetString);
    }

    [Test]
    public void To_plaintext()
    {
        var original = File.ReadAllText("Html/Files/contentful.html", Encoding.UTF8);
        var coded = HtmlContentCoder.Deserialize(original, "contentful.html");

        var plaintext = coded.GetPlaintext();
        Assert.IsNotEmpty(plaintext);
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine(plaintext);
    }

}
