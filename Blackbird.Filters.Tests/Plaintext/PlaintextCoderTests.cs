using Blackbird.Filters.Coders;
using Blackbird.Filters.Content;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blackbird.Filters.Tests.Plaintext;

[TestFixture]
public class PlaintextCoderTests : TestBase
{
    [Test]
    public void Simple()
    {
        var result = Process("Plaintext/Files/simple.txt");

        Assert.That(result.SourceString, Is.EqualTo(result.Original));
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Linebreak()
    {
        var result = Process("Plaintext/Files/linebreak.txt");

        Assert.That(result.SourceString, Is.EqualTo(result.Original));
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Paragraphs()
    {
        var result = Process("Plaintext/Files/paragraphs.txt");

        Assert.That(result.SourceString, Is.EqualTo(result.Original));
        Assert.That(result.Source.TextUnits.Count(), Is.EqualTo(15));
    }
}
