using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Helpers;

/// <summary>Works out how many people finished each event (for "47th out of 312").</summary>
public static class ResultStats
{
    public static async Task<Dictionary<int, int>> FinisherCountsAsync(RaceDayDbContext db, IEnumerable<int> eventIds)
    {
        var ids = eventIds.Distinct().ToList();

        var finishedEventIds = await db.Results
            .Where(r => r.Status == "Finished" && ids.Contains(r.Enrolment!.Category!.EventId))
            .Select(r => r.Enrolment!.Category!.EventId)
            .ToListAsync();

        return finishedEventIds
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
