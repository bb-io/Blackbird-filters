using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;

namespace Blackbird.Filters.Tests.Other;

[TestFixture]
public class MetadataTests : TestBase
{
    [Test]
    public void Set()
    {
        var transformation = new Transformation("en");

        var empty = transformation.MetaData.Get("Test");
        Assert.That(empty, Is.Null);

        transformation.MetaData.Set("Test", "ok");

        Assert.That(transformation.MetaData.Count, Is.EqualTo(1));
        Assert.That(transformation.MetaData.Get("Test"), Is.EqualTo("ok"));

        transformation.MetaData.Set("Test", "ok 2");

        Assert.That(transformation.MetaData.Count, Is.EqualTo(1));
        Assert.That(transformation.MetaData.Get("Test"), Is.EqualTo("ok 2"));

        transformation.MetaData.Set(["Blackbird"], "Test", "ok 3");

        Assert.That(transformation.MetaData.Count, Is.EqualTo(2));
        Assert.That(transformation.MetaData.Get(["Blackbird"], "Test"), Is.EqualTo("ok 3"));

        transformation.MetaData.Set(["Blackbird"], "Test", "ok 4");

        Assert.That(transformation.MetaData.Count, Is.EqualTo(2));
        Assert.That(transformation.MetaData.Get(["Blackbird"], "Test"), Is.EqualTo("ok 4"));
    }
}
