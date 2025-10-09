using Blackbird.Filters.Transformations;

namespace Blackbird.Filters.Extensions;
public static class UnitExtensions
{   

    public static IEnumerable<IEnumerable<(Unit Unit, Segment Segment)>> Batch(this IEnumerable<Unit> units, int batchSize = 1500, Func<Segment, bool>? segmentFilter = null)
    {
        if (batchSize <= 0) throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));

        var batch = new List<(Unit, Segment)>(batchSize);

        foreach (var unit in units)
        {
            foreach (var segment in unit.Segments) 
            {
                if (segmentFilter != null && !segmentFilter(segment))
                    continue;

                batch.Add((unit, segment));

                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<(Unit, Segment)>(batchSize);
                }
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    public static IEnumerable<(Unit, IEnumerable<(Segment Segment, T Result)>)> Process<T>(this IEnumerable<IEnumerable<(Unit Unit, Segment Segment)>> batches, Func<IEnumerable<(Unit Unit, Segment Segment)>, IEnumerable<T>> func)
    {
        foreach (var batch in batches)
        {
            var items = batch.ToList();

            var results = func(items)?.ToList() ?? throw new InvalidOperationException("The processing function returned null.");

            if (results.Count != items.Count) throw new InvalidOperationException("The processing function must return exactly one result per (Unit, Segment) in the batch.");

            var paired = Enumerable.Range(0, items.Count).Select(i => (items[i].Unit, items[i].Segment, Value: results[i]));

            foreach (var group in paired.GroupBy(p => p.Unit))
            {
                var segResults = group.Select(p => (p.Segment, p.Value)).ToList();
                yield return (group.Key, segResults);
            }
        }
    }

    public async static Task<IEnumerable<(Unit Unit, IEnumerable<(Segment Segment, T Result)> Results)>> Process<T>(this IEnumerable<IEnumerable<(Unit Unit, Segment Segment)>> batches, Func<IEnumerable<(Unit Unit, Segment Segment)>, Task<IEnumerable<T>>> func)
    {
        var output = new List<(Unit, IEnumerable<(Segment, T)>)>();

        foreach (var batch in batches)
        {
            if (batch == null) continue;
            var items = batch.ToList();

            var resultsEnumerable = await func(items).ConfigureAwait(false) ?? throw new InvalidOperationException("The processing function returned null.");

            var results = resultsEnumerable.ToList();
            if (results.Count != items.Count) throw new InvalidOperationException("The processing function must return exactly one result per (Unit, Segment) in the batch.");

            var paired = Enumerable.Range(0, items.Count).Select(i => (items[i].Unit, items[i].Segment, Value: results[i]));

            foreach (var group in paired.GroupBy(p => p.Unit))
            {
                var segResults = group.Select(p => (p.Segment, p.Value)).ToList();
                output.Add((group.Key, segResults));
            }
        }

        return output;
    }

    public async static Task<IEnumerable<(Unit Unit, IEnumerable<(Segment Segment, T Result)> Results)>> ProcessParallel<T>(
        this IEnumerable<IEnumerable<(Unit Unit, Segment Segment)>> batches,
        Func<IEnumerable<(Unit Unit, Segment Segment)>, Task<IEnumerable<T>>> func,
        int maxDegreeOfParallelism = int.MaxValue)
    {
        if (batches is null) return Enumerable.Empty<(Unit, IEnumerable<(Segment, T)>)>();

        var batchList = batches.ToList();
        if (batchList.Count == 0) return Enumerable.Empty<(Unit, IEnumerable<(Segment, T)>)>();

        using var throttler = new SemaphoreSlim(maxDegreeOfParallelism);

        var tasks = batchList
            .Select((batch, index) => ProcessOne(index, batch))
            .ToArray();

        var perBatchResults = await Task.WhenAll(tasks).ConfigureAwait(false);

        return perBatchResults
            .OrderBy(r => r.Index)
            .SelectMany(r => r.Results);

        async Task<(int Index, List<(Unit Unit, IEnumerable<(Segment Segment, T Result)> Results)> Results)> ProcessOne(
            int index,
            IEnumerable<(Unit Unit, Segment Segment)>? batch)
        {
            if (batch is null) return (index, new List<(Unit, IEnumerable<(Segment, T)>)>());

            await throttler.WaitAsync().ConfigureAwait(false);
            try
            {
                var items = batch.ToList();
                if (items.Count == 0) return (index, new List<(Unit, IEnumerable<(Segment, T)>)>());

                var resultsEnumerable = await func(items).ConfigureAwait(false) ?? throw new InvalidOperationException("The processing function returned null.");

                var results = resultsEnumerable.ToList();
                if (results.Count != items.Count) throw new InvalidOperationException("The processing function must return exactly one result per (Unit, Segment) in the batch.");

                var paired = Enumerable.Range(0, items.Count).Select(i => (items[i].Unit, items[i].Segment, Value: results[i]));

                var grouped = paired
                    .GroupBy(p => p.Unit)
                    .Select(g =>
                    (
                        Unit: g.Key,
                        Results: (IEnumerable<(Segment Segment, T Result)>)g
                            .Select(p => (p.Segment, p.Value))
                            .ToList()
                    ))
                    .ToList();

                return (index, grouped);
            }
            finally
            {
                throttler.Release();
            }
        }
    }
}
