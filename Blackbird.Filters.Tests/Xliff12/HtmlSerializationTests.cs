using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Tests.Html;
using Blackbird.Filters.Tests.Models;

namespace Blackbird.Filters.Tests.Xliff12;

[TestFixture]
public class HtmlSerializationTests : HtmlTestBase
{
    [Test]
    public void SerializeSimpleHtmlToXliff12()
    {
        // Arrange & Act
        var (html, content, returned) = ProcessSource("Xliff12/Files/Html/simple.html", XliffVersion.Xliff1);

        // Assert
        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
    }
    
    [Test]
    public void SerializeHtmlWithHypertextToXliff12()
    {
        // Arrange & Act
        var (html, content, returned) = ProcessSource("Xliff12/Files/Html/hypertext.html", XliffVersion.Xliff1);

        // Assert
        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(3));
    }
    
    [Test]
    public void SerializeContentfulHtmlWithHypertextToXliff12()
    {
        // Arrange & Act
        var (html, content, returned) = ProcessSource("Html/Files/contentful.html", XliffVersion.Xliff1);

        // Assert
        HtmlAssert.AreEqual(html, returned);
        Assert.That(content.TextUnits.Count(), Is.EqualTo(23));
    }
}