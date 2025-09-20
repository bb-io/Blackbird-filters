using AngleSharp.Diffing;
using HtmlAgilityPack;

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

    public static bool LooksLikeHtmlNode(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        var doc = new HtmlDocument
        {
            OptionCheckSyntax = true,
            OptionFixNestedTags = true
        };

        doc.LoadHtml(input);

        if (doc.ParseErrors != null && doc.ParseErrors.Any())
            return false;

        // Must produce at least one element node (not just text).
        return doc.DocumentNode
                  .Descendants()
                  .Any(n => n.NodeType == HtmlNodeType.Element);
    }
}
