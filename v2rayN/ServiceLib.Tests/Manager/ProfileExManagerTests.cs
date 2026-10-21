using ServiceLib.Manager;

namespace ServiceLib.Tests.Manager;

public class ProfileExManagerTests
{
    [Test]
    public async Task ConcurrentDelayResults_AreAllRecorded()
    {
        // Delay tests report results from many threads at once.
        var ids = Enumerable.Range(0, 200).Select(i => $"concurrent-test-{Guid.NewGuid():N}-{i}").ToList();
        await Parallel.ForEachAsync(Enumerable.Range(0, 4000), new ParallelOptions { MaxDegreeOfParallelism = 16 },
            (i, _) =>
            {
                var id = ids[i % ids.Count];
                ProfileExManager.Instance.SetTestDelay(id, 100 + i % ids.Count);
                return ValueTask.CompletedTask;
            });

        var items = await ProfileExManager.Instance.GetProfileExs();
        foreach (var (id, index) in ids.Select((id, index) => (id, index)))
        {
            var matches = items.Where(item => item.IndexId == id).ToList();
            await matches.Count.Should().BeEqualTo(1);
            await matches[0].Delay.Should().BeEqualTo(100 + index);
        }
    }
}
