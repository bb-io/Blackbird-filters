using System.Text;
using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;

namespace Blackbird.Filters.Tests.Html;

public abstract class HtmlTestBase : TestBase
{
    protected (string, CodedContent, string) ProcessSource(string file, XliffVersion xliffVersion = XliffVersion.Xliff2)
    {
        var html = File.ReadAllText(file, Encoding.UTF8);
        var content = HtmlContentCoder.Deserialize(html);
        var transformation = content.CreateTransformation("en", "nl");
        var serialized = transformation.Serialize(xliffVersion);
        var deserialized = Transformation.Parse(serialized);
        var returned = deserialized.Source().Serialize();
        DisplayXml(serialized);
        Console.WriteLine("------");
        DisplayHtml(returned);

        return (html, content, returned);
    }

    protected (string, CodedContent, string) ProcessTarget(string file, XliffVersion xliffVersion = XliffVersion.Xliff2)
    {
        var html = File.ReadAllText(file, Encoding.UTF8);
        var content = HtmlContentCoder.Deserialize(html);
        var transformation = content.CreateTransformation("en", "nl");
        var serialized = transformation.Serialize(xliffVersion);
        serialized = PseudoTranslateXliff(serialized, xliffVersion);
        var deserialized = Transformation.Parse(serialized);
        var returned = deserialized.Target().Serialize();
        DisplayXml(serialized);
        Console.WriteLine("------");
        DisplayHtml(returned);

        return (html, content, returned);
    }

    protected string TranslateFile(string fileContent)
    {
        // File content can be either HTML or XLIFF (more formats to follow soon)
        var transformation = Transformation.Parse(fileContent);

        foreach(var segment in transformation.GetSegments()) // You can also add .batch() to batch segments
        {
            // Implement API calls here
            segment.SetTarget(segment.GetSource() + " - Translated!"); 

            // More state manipulations can be performed here
            segment.State = SegmentState.Translated; 
        }
        return transformation.Serialize();
    }

    protected string PseudoTranslateXliff(string xliff, XliffVersion xliffVersion = XliffVersion.Xliff2)
    {
        var transformation = Transformation.Parse(xliff);
        foreach (var segment in transformation.GetSegments())
        {
            segment.SetTarget(segment.GetSource() + "TRANSLATED");
            segment.State = SegmentState.Translated;
        }
        
        return transformation.Serialize(xliffVersion);
    }
}