using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using HtmlAgilityPack;
using System.Xml.Serialization;

namespace Blackbird.Filters.Tests.Html;

[TestFixture]
public class UnitBuilderTests : TestBase
{

    [Test]
    public void Plain_text()
    {
        // Arrange
        var original = "Plain text";
        var doc = new HtmlDocument();
        doc.LoadHtml(original);

        // Act
        var units = HtmlContentCoder.BuildUnits(doc.DocumentNode);
        var html = units.FirstOrDefault()!.GetCodedText();

        Display(units);
        Display(html);

        // Assert
        Assert.That(html, Is.EqualTo(original));
        Assert.That(units.FirstOrDefault()!.Parts.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Single_br()
    {
        // Arrange
        var original = "Sentence one.<br>Sentence two.";
        var doc = new HtmlDocument();
        doc.LoadHtml(original);

        // Act
        var units = HtmlContentCoder.BuildUnits(doc.DocumentNode);
        var html = units.FirstOrDefault()!.GetCodedText();

        Display(units);
        Display(html);

        // Assert
        Assert.That(html, Is.EqualTo(original));
        Assert.That(units.FirstOrDefault()!.Parts.Count(), Is.EqualTo(3));
    }

    [Test]
    public void Simple_span()
    {
        // Arrange
        var original = "Hello <b>world</b>";
        var doc = new HtmlDocument();
        doc.LoadHtml(original);

        // Act
        var units = HtmlContentCoder.BuildUnits(doc.DocumentNode);
        var html = units.FirstOrDefault()!.GetCodedText();

        Display(units);
        Display(html);

        // Assert
        Assert.That(units.FirstOrDefault()!.Parts.Count(), Is.EqualTo(4));
        Assert.That(html, Is.EqualTo(original));
    }

    [Test]
    public void Double_span()
    {
        // Arrange
        var original = "<i>Hello</i><b>world</b>";
        var doc = new HtmlDocument();
        doc.LoadHtml(original);

        // Act
        var units = HtmlContentCoder.BuildUnits(doc.DocumentNode);
        var html = units.FirstOrDefault()!.GetCodedText();

        Display(units);
        Display(html);

        // Assert
        Assert.That(html, Is.EqualTo(original));
        Assert.That(units.FirstOrDefault()!.Parts.Count(), Is.EqualTo(6));
    }

    [Test]
    public void Nested_span()
    {
        // Arrange
        var original = "Hey,<i>Hello<b>world</b>and the rest</i>";
        var doc = new HtmlDocument();
        doc.LoadHtml(original);

        // Act
        var units = HtmlContentCoder.BuildUnits(doc.DocumentNode);
        var html = units.FirstOrDefault()!.GetCodedText();

        Display(units);
        Display(html); ;

        // Assert
        Assert.That(html, Is.EqualTo(original));
        Assert.That(units.FirstOrDefault()!.Parts.Count(), Is.EqualTo(8));
    }
}
