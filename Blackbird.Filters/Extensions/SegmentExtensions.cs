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

    public static IEnumerable<(Segment, T)> Process<T>(this IEnumerable<IEnumerable<Segment>> batches, Func<IEnumerable<Segment>, IEnumerable<T>> func)
    {
        foreach (var batch in batches)
        {
            var fnResult = func(batch).ToList();
            var batchAsArray = batch.ToArray();
            for (int i = 0; i < fnResult.Count; i++)
            {
                var segment = batchAsArray[i];
                var result = fnResult[i];
                yield return (segment, result);
            }
        }
    }

    public async static Task<IEnumerable<(Segment, T)>> Process<T>(this IEnumerable<IEnumerable<Segment>> batches, Func<IEnumerable<Segment>, Task<IEnumerable<T>>> func)
    {
        var finalResult = new List<(Segment, T)>();
        foreach (var batch in batches)
        {
            var fnResult = (await func(batch)).ToList();
            var batchAsArray = batch.ToArray();
            for (int i = 0; i < fnResult.Count; i++)
            {
                var segment = batchAsArray[i];
                var result = fnResult[i];
                finalResult.Add((segment, result));
            }
        }
        return finalResult;
    }
}