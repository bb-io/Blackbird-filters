using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text;
using Blackbird.Filters.Tests.Models;
using Blackbird.Filters.Xliff.Xliff1;

namespace Blackbird.Filters.Tests;

public abstract class TestBase
{
    protected void Display(object? value)
    {
        Console.OutputEncoding = Encoding.UTF8;
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
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine(rawXml);
    }

    protected ProcessResult Process(string filePath, XliffVersion? xliffVersion = null)
    {
        var original = File.ReadAllText(filePath, Encoding.UTF8);
        var transformation = Transformation.Parse(original, Path.GetFileName(filePath));
        transformation.TargetLanguage = "nl";

        var source = transformation.Source();
        var sourceString = source.Serialize();

        var serialized = SerializeBasedOnXliffVersion(transformation, xliffVersion);
        serialized = PseudoTranslateXliff(serialized, transformation.XliffFileName);
        var deserialized = Transformation.Parse(serialized, transformation.XliffFileName);

        var target = deserialized.Target();
        var targetString = target.Serialize();

        DisplayXml(serialized);
        Console.WriteLine("------");

        if (HtmlContentCoder.IsHtml(targetString))
        {
            DisplayHtml(targetString);
        } 
        else
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
    
    private string SerializeBasedOnXliffVersion(Transformation transformation, XliffVersion? xliffVersion)
    {
        if (xliffVersion == XliffVersion.Xliff1)
        {
            return Xliff1Serializer.Serialize(transformation);
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