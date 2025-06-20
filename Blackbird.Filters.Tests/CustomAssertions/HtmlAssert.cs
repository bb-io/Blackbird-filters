using AngleSharp.Diffing;
using System.Xml.Linq;

namespace Blackbird.Filters.Tests.CustomAssertions;
public static class HtmlAssert
{
    public static void AreEqual(string expectedHtml, string actualHtml, string? message = null)
    {
        var diffs = DiffBuilder.Compare(expectedHtml).WithTest(actualHtml).Build();

        if (diffs.Count() != 0)
        {
            Assert.Fail(message ?? $"Html documents are not equal:\n{diffs}");
        }        
    }

    public static void AreNotEqual(string expectedHtml, string actualHtml, string? message = null)
    {
        var diffs = DiffBuilder.Compare(expectedHtml).WithTest(actualHtml).Build();

        if (diffs.Count() == 0)
        {
            Assert.Fail(message ?? $"Html documents are equal!");
        }
    }
}
