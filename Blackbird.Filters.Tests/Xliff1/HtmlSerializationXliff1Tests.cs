using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Tests.Models;

namespace Blackbird.Filters.Tests.Xliff1;

[TestFixture]
public class HtmlSerializationXliff1Tests : TestBase
{
    [TestCase("simple.html")]
    [TestCase("hypertext.html")]
    [TestCase("contentful.html")]
    public void SerializeSimpleHtmlToXliff12(string fileName)
    {
        // Arrange & Act
        var processResult = Process($"Xliff12/Files/Html/{fileName}", XliffVersion.Xliff1);

        // Assert
        var targetHtml = processResult.Target.Serialize();
        targetHtml = targetHtml.Replace("TRANSLATED", string.Empty);
        HtmlAssert.AreEqual(processResult.Original, targetHtml);
        Assert.That(processResult.Target.TextUnits.Any(), Is.True);
    }
}