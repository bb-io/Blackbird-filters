using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Transformations;
using System.Text;

namespace Blackbird.Filters.Tests.Html;

[TestFixture]
public class HtmlCoderTests : HtmlTestBase
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

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Empty(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/empty.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(0));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Simple(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/simple.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void With_br(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/with_br.html", version);
        
        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(1));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Inline_tags(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/inline_tags.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(2));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Floating_text(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/floating_text.html", version);
      
        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Img_alt_text(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/img_alt_text.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
        Assert.IsTrue(content.TextUnits.Any(x => x.GetCodedText().Contains("Girl with a jacket", StringComparison.InvariantCultureIgnoreCase)));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Button_subflows(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/subflows.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
        Assert.IsTrue(content.TextUnits.Any(x => x.GetCodedText().Contains("Click here to start!", StringComparison.InvariantCultureIgnoreCase)));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Img_title_attr(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/img_title_attr.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsTrue(content.TextUnits.Any(x => x.GetCodedText().Contains("I'm a tooltip", StringComparison.InvariantCultureIgnoreCase)));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Comments(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/comments.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(1));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Script(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/script.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsFalse(content.TextUnits.Any(x => x.GetCodedText().Contains("function", StringComparison.InvariantCultureIgnoreCase)));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Meta_content(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/meta_content.html", version);

        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(4));
        Assert.IsTrue(content.TextUnits.Any(x => x.GetCodedText().Contains("Free Web tutorials", StringComparison.InvariantCultureIgnoreCase)));
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Full_example(XliffVersion version)
    {
        var (html, content, returned) = ProcessSource("Html/Files/contentful.html", version);
        HtmlAssert.AreEqual(html, returned);
    }

    [TestCase(XliffVersion.Xliff2)]
    [TestCase(XliffVersion.Xliff1)]
    public void Full_translated(XliffVersion version)
    {
        var (html, content, returned) = ProcessTarget("Html/Files/contentful.html", version);
        HtmlAssert.AreNotEqual(html, returned);
    }
}
