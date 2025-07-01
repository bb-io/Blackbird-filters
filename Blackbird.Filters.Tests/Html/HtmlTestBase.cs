using System.Text;
using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Tests.Models;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff12;
using Blackbird.Filters.Xliff.Xliff2;

namespace Blackbird.Filters.Tests.Html;

public abstract class HtmlTestBase : TestBase
{
    protected (string, CodedContent, string) ProcessSource(string file, XliffVersion xliffVersion = XliffVersion.Xliff2)
    {
        var html = File.ReadAllText(file, Encoding.UTF8);
        var content = HtmlContentCoder.Deserialize(html);
        var transformation = content.CreateTransformation("en", "nl");
        var serialized = SerializeBasedOnXliffVersion(transformation, xliffVersion);
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
        var serialized = SerializeBasedOnXliffVersion(transformation, xliffVersion);
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
        
        return SerializeBasedOnXliffVersion(transformation, xliffVersion);
    }

    private string SerializeBasedOnXliffVersion(Transformation transformation, XliffVersion xliffVersion)
    {
        if (xliffVersion == XliffVersion.Xliff1)
        {
            return Xliff12Serializer.Serialize(transformation);
        }

        return Xliff2Serializer.Serialize(transformation);
    }
}