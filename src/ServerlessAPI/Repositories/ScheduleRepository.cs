using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;

namespace ServerlessAPI.Repositories;

public sealed class ScheduleRepository(SanLorenzoDbContext db) : IScheduleRepository
{
    [Tracing(SegmentName = "List schedule")]
    public async Task<ScheduleResponse> GetScheduleAsync(int teacherId, CancellationToken ct = default)
    {
        // Times are formatted in memory: SQL Server cannot translate ToString("HH:mm").
        var rows = await db.ScheduleSlots
            .AsNoTracking()
            .Where(s => s.Course.TeacherId == teacherId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.Course.Name,
                s.Course.GradeLevel,
                s.Course.Section,
                s.StartTime,
                s.EndTime,
                s.DayOfWeek,
                s.Icon,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var classes = rows
            .Select(r => new ScheduledClass(
                r.Id,
                r.Name,
                r.GradeLevel,
                r.Section,
                r.StartTime.ToString("HH:mm"),
                r.EndTime.ToString("HH:mm"),
                r.DayOfWeek,
                r.Icon))
            .ToList();

        return new ScheduleResponse(teacherId, classes);
    }
}
