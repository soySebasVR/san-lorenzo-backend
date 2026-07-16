using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class CoordinatorAnnouncementRepository(
    SanLorenzoDbContext db) : ICoordinatorAnnouncementRepository
{
    [Tracing(SegmentName = "List coordinator announcements")]
    public async Task<IReadOnlyList<CoordinatorAnnouncementResponse>> GetAsync(
        int userId,
        string? status,
        CancellationToken ct = default)
    {
        var query = db.CoordinatorAnnouncements
            .AsNoTracking()
            .Include(a => a.Recipients)
            .Where(a => a.CreatedByUserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsedStatus = ParseStatus(
                status,
                allowCancelled: true);

            query = query.Where(a => a.Status == parsedStatus);
        }

        var announcements = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return announcements
            .Select(MapResponse)
            .ToList();
    }

    [Tracing(SegmentName = "Create coordinator announcement")]
    public async Task<CoordinatorAnnouncementResponse> CreateAsync(
        int userId,
        CreateCoordinatorAnnouncementRequest request,
        CancellationToken ct = default)
    {
        await EnsureActiveCoordinatorAsync(userId, ct);

        var status = ParseStatus(
            request.Status,
            allowCancelled: false);

        ValidateSchedule(status, request.ScheduledAt);

        var now = DateTime.UtcNow;

        var announcement = new CoordinatorAnnouncement
        {
            CreatedByUserId = userId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            ScheduledAt = NormalizeDateTime(request.ScheduledAt),
            SentAt = status == CoordinatorAnnouncementStatus.Sent
                ? now
                : null,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var recipient in BuildRecipients(request.Recipients))
            announcement.Recipients.Add(recipient);

        db.CoordinatorAnnouncements.Add(announcement);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return MapResponse(announcement);
    }

    [Tracing(SegmentName = "Update coordinator announcement")]
    public async Task<CoordinatorAnnouncementResponse> UpdateAsync(
        int userId,
        int announcementId,
        UpdateCoordinatorAnnouncementRequest request,
        CancellationToken ct = default)
    {
        var announcement = await db.CoordinatorAnnouncements
            .Include(a => a.Recipients)
            .FirstOrDefaultAsync(
                a => a.Id == announcementId
                     && a.CreatedByUserId == userId,
                ct)
            .ConfigureAwait(false);

        if (announcement is null)
            throw new NotFoundException(
                $"Coordinator announcement {announcementId} was not found.");

        if (announcement.Status is
            CoordinatorAnnouncementStatus.Sent or
            CoordinatorAnnouncementStatus.Cancelled)
        {
            throw new BadRequestException(
                "Sent or cancelled announcements cannot be modified.");
        }

        var newStatus = ParseStatus(
            request.Status,
            allowCancelled: true);

        ValidateSchedule(newStatus, request.ScheduledAt);

        var now = DateTime.UtcNow;

        announcement.Title = request.Title.Trim();
        announcement.Content = request.Content.Trim();
        announcement.ScheduledAt =
            NormalizeDateTime(request.ScheduledAt);
        announcement.Status = newStatus;
        announcement.UpdatedAt = now;

        announcement.SentAt =
            newStatus == CoordinatorAnnouncementStatus.Sent
                ? now
                : null;

        db.CoordinatorAnnouncementRecipients.RemoveRange(
            announcement.Recipients);

        announcement.Recipients.Clear();

        foreach (var recipient in BuildRecipients(request.Recipients))
            announcement.Recipients.Add(recipient);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return MapResponse(announcement);
    }

    private async Task EnsureActiveCoordinatorAsync(
        int userId,
        CancellationToken ct)
    {
        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == userId
                     && u.Role == Role.Coordinator
                     && u.IsActive,
                ct)
            .ConfigureAwait(false);

        if (!exists)
            throw new NotFoundException(
                $"Coordinator user {userId} does not exist or is inactive.");
    }

    private static CoordinatorAnnouncementStatus ParseStatus(
        string value,
        bool allowCancelled)
    {
        if (!Enum.TryParse<CoordinatorAnnouncementStatus>(
                value,
                ignoreCase: true,
                out var status))
        {
            throw new BadRequestException(
                $"Invalid announcement status '{value}'.");
        }

        if (!allowCancelled &&
            status == CoordinatorAnnouncementStatus.Cancelled)
        {
            throw new BadRequestException(
                "A new announcement cannot be created as cancelled.");
        }

        return status;
    }

    private static void ValidateSchedule(
        CoordinatorAnnouncementStatus status,
        DateTime? scheduledAt)
    {
        if (status != CoordinatorAnnouncementStatus.Scheduled)
            return;

        if (scheduledAt is null)
            throw new BadRequestException(
                "ScheduledAt is required when the status is Scheduled.");

        var normalizedDate = NormalizeDateTime(scheduledAt);

        if (normalizedDate <= DateTime.UtcNow)
            throw new BadRequestException(
                "ScheduledAt must be a future date.");
    }

    private static List<CoordinatorAnnouncementRecipient> BuildRecipients(
        IEnumerable<CoordinatorAnnouncementRecipientRequest> requests)
    {
        var requestList = requests.ToList();

        if (requestList.Count == 0)
            throw new BadRequestException(
                "At least one recipient must be provided.");

        var recipients =
            new List<CoordinatorAnnouncementRecipient>();

        var uniqueRecipients =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in requestList)
        {
            if (!Enum.TryParse<CoordinatorRecipientType>(
                    request.TargetType,
                    ignoreCase: true,
                    out var targetType))
            {
                throw new BadRequestException(
                    $"Invalid recipient type '{request.TargetType}'.");
            }

            Role? targetRole = null;

            if (!string.IsNullOrWhiteSpace(request.TargetRole))
            {
                if (!Enum.TryParse<Role>(
                        request.TargetRole,
                        ignoreCase: true,
                        out var parsedRole))
                {
                    throw new BadRequestException(
                        $"Invalid target role '{request.TargetRole}'.");
                }

                targetRole = parsedRole;
            }

            var gradeLevel =
                NormalizeOptional(request.GradeLevel);

            var section =
                NormalizeOptional(request.Section);

            switch (targetType)
            {
                case CoordinatorRecipientType.All:
                    if (targetRole is not null ||
                        gradeLevel is not null ||
                        section is not null)
                    {
                        throw new BadRequestException(
                            "Recipients of type All cannot specify role, grade or section.");
                    }

                    break;

                case CoordinatorRecipientType.Role:
                    if (targetRole is null)
                        throw new BadRequestException(
                            "TargetRole is required for recipients of type Role.");

                    if (gradeLevel is not null || section is not null)
                        throw new BadRequestException(
                            "Recipients of type Role cannot specify grade or section.");

                    break;

                case CoordinatorRecipientType.GradeSection:
                    if (gradeLevel is null || section is null)
                        throw new BadRequestException(
                            "GradeLevel and Section are required for recipients of type GradeSection.");

                    break;

                default:
                    throw new BadRequestException(
                        "Unsupported recipient type.");
            }

            var uniquenessKey =
                $"{targetType}|{targetRole}|{gradeLevel}|{section}";

            if (!uniqueRecipients.Add(uniquenessKey))
                throw new BadRequestException(
                    "Duplicate recipients are not allowed.");

            recipients.Add(
                new CoordinatorAnnouncementRecipient
                {
                    TargetType = targetType,
                    TargetRole = targetRole,
                    GradeLevel = gradeLevel,
                    Section = section,
                });
        }

        if (recipients.Any(
                r => r.TargetType == CoordinatorRecipientType.All) &&
            recipients.Count > 1)
        {
            throw new BadRequestException(
                "A recipient of type All cannot be combined with other recipients.");
        }

        return recipients;
    }

    private static CoordinatorAnnouncementResponse MapResponse(
        CoordinatorAnnouncement announcement)
    {
        return new CoordinatorAnnouncementResponse(
            announcement.Id,
            announcement.Title,
            announcement.Content,
            announcement.ScheduledAt,
            announcement.SentAt,
            announcement.Status.ToString(),
            announcement.CreatedAt,
            announcement.UpdatedAt,
            announcement.Recipients
                .OrderBy(r => r.Id)
                .Select(r =>
                    new CoordinatorAnnouncementRecipientResponse(
                        r.Id,
                        r.TargetType.ToString(),
                        r.TargetRole?.ToString(),
                        r.GradeLevel,
                        r.Section))
                .ToList());
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static DateTime? NormalizeDateTime(DateTime? value)
    {
        if (value is null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
    }
}
