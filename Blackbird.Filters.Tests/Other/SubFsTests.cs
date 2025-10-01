using Blackbird.Filters.Xliff.Xliff2;

namespace Blackbird.Filters.Tests.Other;

[TestFixture]
public class SubFsTests : TestBase
{
    private static void AssertAttributes(Dictionary<string, string> actual, Dictionary<string, string> expected)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count));

        foreach (var key in actual.Keys)
        {
            Console.WriteLine($"{key}: {actual[key]}");
            Assert.That(actual[key], Is.EqualTo(expected[key]));
        }
    }

    [Test]
    public void Basic()
    {
        var attributes = new Dictionary<string, string>
        {
            {"src", "img.jpg" },
        };

        var serialized = SubFsHelper.ToSubFsString(attributes);
        Console.WriteLine(serialized);
        var deserialized = SubFsHelper.ParseSubFsString(serialized);

        AssertAttributes(deserialized, attributes);
    }

    [Test]
    public void Multi()
    {
        var attributes = new Dictionary<string, string>
        {
            {"src", "img.jpg" },
            {"alt", "My Happy Smile" },
            {"title", "Smiling" },
        };

        var serialized = SubFsHelper.ToSubFsString(attributes);
        Console.WriteLine(serialized);
        var deserialized = SubFsHelper.ParseSubFsString(serialized);

        AssertAttributes(deserialized, attributes);
    }

    [Test]
    public void Special_chars()
    {
        var attributes = new Dictionary<string, string>
        {
            {"src", "c:\\docs\\images\\smile.png" },
            {"alt", "My Happy Smile" },
            {"title", "Hey there, how are you?" },
        };

        var serialized = SubFsHelper.ToSubFsString(attributes);
        Console.WriteLine(serialized);
        var deserialized = SubFsHelper.ParseSubFsString(serialized);

        AssertAttributes(deserialized, attributes);
    }
}
