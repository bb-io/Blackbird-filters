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
        var unit = GetUnitFromString(original);
        var html = unit.GetCodedText();

        Display(unit);
        Display(html); ;

        // Assert
        Assert.That(html, Is.EqualTo(original));
        Assert.That(unit.Parts.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Single_br()
    {
        // Arrange
        var original = "Sentence one.<br>Sentence two.";
        var unit = GetUnitFromString(original);
        var html = unit.GetCodedText();

        Display(unit);
        Display(html); ;

        // Assert
        Assert.That(html, Is.EqualTo(original));
        Assert.That(unit.Parts.Count(), Is.EqualTo(3));
    }

    [Test]
    public void Simple_span()
    {
        // Arrange
        var original = "Hello <b>world</b>";
        var unit = GetUnitFromString(original);
        var html = unit.GetCodedText();

        Display(unit);
        Display(html); ;

        // Assert
        Assert.That(unit.Parts.Count(), Is.EqualTo(4));
        Assert.That(html, Is.EqualTo(original));
    }

    [Test]
    public void Double_span()
    {
        // Arrange
        var original = "<i>Hello</i><b>world</b>";
        var unit = GetUnitFromString(original);
        var html = unit.GetCodedText();

        Display(unit);
        Display(html); ;

        // Assert
        Assert.That(html, Is.EqualTo(original));
        Assert.That(unit.Parts.Count(), Is.EqualTo(6));
    }

    [Test]
    public void Nested_span()
    {
        // Arrange
        var original = "Hey,<i>Hello<b>world</b>and the rest</i>";
        var unit = GetUnitFromString(original);
        var html = unit.GetCodedText();

        Display(unit);
        Display(html); ;

        // Assert
        Assert.That(html, Is.EqualTo(original));
        Assert.That(unit.Parts.Count(), Is.EqualTo(8));
    }

    [Test]
    public void Normalized_equivalence()
    {
        var version1 = GetUnitFromString("<p class=\"text center\">Hello <a href=\"http://www.example.com\">world</a></p>");
        var version2 = GetUnitFromString("<p class=\"text p-1 m-2\">Hello <a href=\"http://www.another-example.com\">world</a></p>");

        Display(version1.GetNormalizedText());
        Display(version2.GetNormalizedText());

        Assert.That(version1.GetNormalizedText(), Is.EqualTo(version2.GetNormalizedText()));
    }

    private TextUnit GetUnitFromString(string original)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(original);
        var units = new HtmlContentCoder().BuildUnits(doc.DocumentNode);
        return units.First();
    }
}