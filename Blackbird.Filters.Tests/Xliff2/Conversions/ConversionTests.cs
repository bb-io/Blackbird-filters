using Blackbird.Filters.Coders;
using Blackbird.Filters.Xliff.Xliff2;
using System.ComponentModel.DataAnnotations;

namespace Blackbird.Filters.Tests.Xliff2.Conversions;

[TestFixture]
public class ConversionTests : TestBase
{
    [Test]
    public void FromHTML_contains_startRef()
    {
        var givenHtml = "<i>Hello</i><b>world</b>";

        var content = HtmlContentCoder.Deserialize(givenHtml, "sample.html");
        var transformation = content.CreateTransformation("es");
        var xliff = Xliff2Serializer.Serialize(transformation);

        DisplayXml(xliff);
        Assert.That(xliff, Does.Contain("startRef"));
    }
}
