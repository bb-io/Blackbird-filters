using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using System.Text;

namespace Blackbird.Filters.Tests.Other;

[TestFixture]
public class BatchTests : TestBase
{
    [Test]
    public void Batch()
    {
        var batchSize = 5;
        var xliff = File.ReadAllText("Xliff2/Files/contentful.xliff", Encoding.UTF8);

        var transformation = Transformation.Parse(xliff, "contentful.xliff");

        IEnumerable<string> BatchTranslate(IEnumerable<(Unit Unit, Segment Segment)> batch)
        {
            Assert.That(batch.Count(), Is.LessThanOrEqualTo(batchSize));
            return batch.Select(x => x.Segment.GetSource() + " TRANSLATED");
        }

        var batches = transformation.GetUnits().Batch(batchSize, x => !x.IsIgnorbale).Process(BatchTranslate);

        foreach(var (unit, results) in batches)
        {
            foreach(var (segment, result) in results)
            {
                segment.SetTarget(result);
                segment.State = Enums.SegmentState.Translated;
            }
            unit.Provenance.Translation.Tool = "Pseudo";
        }

        foreach (var unit in transformation.GetUnits())
        {
            Assert.That(unit.Provenance.Translation.Tool == "Pseudo");
            Assert.That(unit.Segments.Where(x => !x.IsIgnorbale).All(x => x.GetTarget().Contains("TRANSLATED")), Is.True);
        }
    }
}
