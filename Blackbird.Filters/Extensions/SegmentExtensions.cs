using Blackbird.Filters.Transformations;

namespace Blackbird.Filters.Extensions;
public static class SegmentExtensions
{
    public static IEnumerable<IEnumerable<Segment>> Batch(this IEnumerable<Segment> segments, int batchSize = 1500)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        }

        return segments
            .Select((unit, index) => new { Segment = unit, Index = index })
            .GroupBy(item => item.Index / batchSize)
            .Select(group => group.Select(item => item.Segment));
    }
}