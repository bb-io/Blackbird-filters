using Blackbird.Filters.Content;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Transformations;
using System.Text;

namespace Blackbird.Filters.Tests.Other;

[TestFixture]
public class RenderTests : TestBase
{
    [Test]
    public void Coded_content()
    {
        var original = File.ReadAllText("Html/Files/contentful.html", Encoding.UTF8);

        var codedContent = CodedContent.Parse(original, "contentful.html");

        foreach(var unit in codedContent.TextUnits)
        {
            Console.WriteLine(unit.GetRenderedText());
            HtmlAssert.LooksLikeHtmlNode(unit.GetRenderedText());
        }
    }

    [Test]
    public void Source()
    {
        var original = File.ReadAllText("Html/Files/contentful.html", Encoding.UTF8);

        var transformation = Transformation.Parse(original, "contentful.html");

        foreach (var unit in transformation.GetUnits())
        {
            Console.WriteLine(unit.GetSource().GetRenderedText());
            HtmlAssert.LooksLikeHtmlNode(unit.GetSource().GetRenderedText());
        }
    }


    [Test]
    public void Target()
    {
        var original = File.ReadAllText("Html/Files/contentful.html", Encoding.UTF8);

        var transformation = Transformation.Parse(original, "contentful.html");

        foreach (var unit in transformation.GetUnits())
        {
            foreach(var segment in unit.Segments)
            {
                segment.SetTarget(segment.GetSource() + " - TRANSLATED");
            }
            Console.WriteLine(unit.GetTarget().GetRenderedText());
            HtmlAssert.LooksLikeHtmlNode(unit.GetTarget().GetRenderedText());
        }
    }
}
