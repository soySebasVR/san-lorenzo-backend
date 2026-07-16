using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class AnnouncementRepository(SanLorenzoDbContext db) : IAnnouncementRepository
{
    [Tracing(SegmentName = "Send announcement")]
    public async Task<AnnouncementResponse> SendAnnouncementAsync(
        int teacherId, SendAnnouncementRequest request, CancellationToken ct = default)
    {
        if (request.CourseId is { } targetCourseId)
        {
            var ownsCourse = await db.Courses
                .AsNoTracking()
                .AnyAsync(c => c.Id == targetCourseId && c.TeacherId == teacherId, ct)
                .ConfigureAwait(false);

            if (!ownsCourse)
                throw new ForbiddenException("Course does not belong to this teacher.");
        }

        // Recipients are students in sections this teacher teaches.
        var recipients = db.Students
            .AsNoTracking()
            .Where(s => db.Courses.Any(c => c.TeacherId == teacherId
                                            && c.GradeLevel == s.GradeLevel
                                            && c.Section == s.Section));

        // Targeting a course narrows to that course's section.
        if (request.CourseId is { } courseFilter)
        {
            recipients = recipients.Where(
                s => db.Courses.Any(c => c.Id == courseFilter
                                         && c.GradeLevel == s.GradeLevel
                                         && c.Section == s.Section));
        }

        if (!string.IsNullOrWhiteSpace(request.GradeLevel))
            recipients = recipients.Where(s => s.GradeLevel == request.GradeLevel);

        if (!string.IsNullOrWhiteSpace(request.Section))
            recipients = recipients.Where(s => s.Section == request.Section);

        var recipientCount = await recipients
            .CountAsync(ct)
            .ConfigureAwait(false);

        var announcement = new Announcement
        {
            TeacherId = teacherId,
            Subject = request.Subject,
            Message = request.Message,
            Section = request.Section,
            GradeLevel = request.GradeLevel,
            CourseId = request.CourseId,
            RecipientCount = recipientCount,
            SentAt = DateTime.UtcNow,
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new AnnouncementResponse(
            announcement.Id,
            announcement.Subject,
            announcement.RecipientCount,
            announcement.SentAt);
    }
}
