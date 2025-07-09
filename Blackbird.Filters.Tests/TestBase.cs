using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text;

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

    protected ProcessResult Process(string filePath)
    {
        var original = File.ReadAllText(filePath, Encoding.UTF8);
        var transformation = Transformation.Parse(original, Path.GetFileName(filePath));
        transformation.SourceLanguage = "en";
        transformation.TargetLanguage = "nl";

        var source = transformation.Source();
        var sourceString = source.Serialize();

        var serialized = transformation.Serialize();
        serialized = PseudoTranslateXliff(serialized, transformation.XliffFileName);
        var deserialized = Transformation.Parse(serialized, transformation.XliffFileName);

        var target = deserialized.Target();
        var targetString = target.Serialize();

        DisplayXml(serialized);
        Console.WriteLine("------");

        if (HtmlContentCoder.IsHtml(targetString))
        {
            DisplayHtml(targetString);
        } else
        {
            Display(targetString);
        }
        DisplayHtml(targetString);

        return new ProcessResult(original, transformation, source, target, sourceString, targetString);
    }

    protected string PseudoTranslateXliff(string xliff, string fileName)
    {
        var transformation = Transformation.Parse(xliff, fileName);
        foreach (var segment in transformation.GetSegments().Where(x => !x.IsIgnorbale))
        {
            segment.SetTarget(segment.GetSource() + "TRANSLATED");
            segment.State = SegmentState.Translated;
        }
        return transformation.Serialize();
    }
}

public record ProcessResult(
    string Original,
    Transformation Transformation,
    CodedContent Source,
    CodedContent Target,
    string SourceString,
    string TargetString
);