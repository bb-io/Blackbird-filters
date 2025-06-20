using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Blackbird.Filters.Tests;
public abstract class TestBase
{
    protected void Display(object? value)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var settings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
        };
        var json = JsonConvert.SerializeObject(value, Formatting.Indented, settings);
        Console.WriteLine(json);
    }

    protected void DisplayHtml(string rawHtml)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(rawHtml);
        Console.WriteLine(doc.DocumentNode.OuterHtml);
    }

    protected void DisplayXml(string rawXml)
    {
        Console.WriteLine(rawXml);
    }
}
