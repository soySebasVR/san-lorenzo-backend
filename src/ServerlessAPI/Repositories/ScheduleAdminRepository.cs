using System.Globalization;
using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class ScheduleAdminRepository(SanLorenzoDbContext db) : IScheduleAdminRepository
{
    [Tracing(SegmentName = "Admin list schedule")]
    public async Task<IReadOnlyList<AdminScheduleSlot>> ListAsync(CancellationToken ct = default)
    {
        var rows = await db.ScheduleSlots
            .AsNoTracking()
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.CourseId,
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

        return rows
            .Select(r => new AdminScheduleSlot(
                r.Id, r.CourseId, r.Name, r.GradeLevel, r.Section,
                r.StartTime.ToString("HH:mm"), r.EndTime.ToString("HH:mm"), r.DayOfWeek, r.Icon))
            .ToList();
    }

    [Tracing(SegmentName = "Create schedule slot")]
    public async Task<AdminScheduleSlot> CreateAsync(SaveScheduleSlotRequest request, CancellationToken ct = default)
    {
        var course = await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.CourseId)
            .Select(c => new { c.Name, c.GradeLevel, c.Section })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new ForbiddenException($"Course {request.CourseId} does not exist.");

        var start = TimeOnly.ParseExact(request.StartTime, "HH:mm", CultureInfo.InvariantCulture);
        var end = TimeOnly.ParseExact(request.EndTime, "HH:mm", CultureInfo.InvariantCulture);

        if (end <= start)
            throw new ForbiddenException("End time must be after start time.");

        var slot = new ScheduleSlot
        {
            CourseId = request.CourseId,
            StartTime = start,
            EndTime = end,
            DayOfWeek = request.DayOfWeek,
            Icon = request.Icon,
        };

        db.ScheduleSlots.Add(slot);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new AdminScheduleSlot(
            slot.Id, request.CourseId, course.Name, course.GradeLevel, course.Section,
            slot.StartTime.ToString("HH:mm"), slot.EndTime.ToString("HH:mm"), slot.DayOfWeek, slot.Icon);
    }

    [Tracing(SegmentName = "Delete schedule slot")]
    public async Task<bool> DeleteAsync(int slotId, CancellationToken ct = default)
    {
        var slot = await db.ScheduleSlots.FirstOrDefaultAsync(s => s.Id == slotId, ct).ConfigureAwait(false);
        if (slot is null)
            return false;

        db.ScheduleSlots.Remove(slot);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
