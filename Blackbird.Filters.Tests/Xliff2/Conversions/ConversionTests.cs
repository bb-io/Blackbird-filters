using Blackbird.Filters.Coders;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Transformations.Tags;
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

    [Test]
    public void FromHTML_translation_inline_ids_correct()
    {
        var givenHtml = "<i>Hello</i><b>world</b>";

        var xliff = HtmlContentCoder.Deserialize(givenHtml, "sample.html").CreateTransformation("es").Serialize();

        var translation = PseudoTranslateXliff(xliff, "sample.html.xlf");
        var returned = Transformation.Parse(translation, "sample.html.xlf");
        var finalXliff = returned.Serialize(); 

        DisplayXml(finalXliff);

        var firstTarget = returned.GetSegments().First().Target;
        var firstInlineTag = firstTarget.OfType<StartTag>().ToList()[0];
        var secondInlineTag = firstTarget.OfType<StartTag>().ToList()[1];

        Assert.That(firstInlineTag.Id, Is.EqualTo("1"));
        Assert.That(secondInlineTag.Id, Is.EqualTo("2"));
        Assert.That(finalXliff, Does.Contain("startRef"));
    }

    [Test]
    public void FromHTML_translation_inline_ids_same_tag_correct()
    {
        var givenHtml = "<i>Hello</i><i>world</i>";

        var xliff = HtmlContentCoder.Deserialize(givenHtml, "sample.html").CreateTransformation("es").Serialize();

        var translation = PseudoTranslateXliff(xliff, "sample.html.xlf");
        var returned = Transformation.Parse(translation, "sample.html.xlf");
        var finalXliff = returned.Serialize();

        DisplayXml(finalXliff);

        var firstTarget = returned.GetSegments().First().Target;
        var firstInlineTag = firstTarget.OfType<StartTag>().ToList()[0];
        var secondInlineTag = firstTarget.OfType<StartTag>().ToList()[1];

        Assert.That(firstInlineTag.Id, Is.EqualTo("1"));
        Assert.That(secondInlineTag.Id, Is.EqualTo("2"));
        Assert.That(finalXliff, Does.Contain("startRef"));
    }
}
