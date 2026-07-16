using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class BroadcastRepository(SanLorenzoDbContext db) : IBroadcastRepository
{
    [Tracing(SegmentName = "Create broadcast")]
    public async Task<BroadcastResponse> CreateAsync(CreateBroadcastRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<BroadcastAudience>(request.Audience, ignoreCase: true, out var audience))
            throw new ForbiddenException($"Unknown audience '{request.Audience}'.");

        var broadcast = new Broadcast
        {
            Subject = request.Subject,
            Body = request.Body,
            Audience = audience,
            GradeLevel = string.IsNullOrWhiteSpace(request.GradeLevel) ? null : request.GradeLevel,
            ScheduledFor = request.ScheduledFor,
            CreatedAt = DateTime.UtcNow,
        };

        db.Broadcasts.Add(broadcast);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ToResponse(broadcast);
    }

    [Tracing(SegmentName = "List broadcasts")]
    public async Task<IReadOnlyList<BroadcastResponse>> ListAsync(CancellationToken ct = default) =>
        await db.Broadcasts
            .AsNoTracking()
            .OrderByDescending(b => b.ScheduledFor)
            .Select(b => new BroadcastResponse(
                b.Id, b.Subject, b.Body, b.Audience.ToString(), b.GradeLevel, b.ScheduledFor, b.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

    private static BroadcastResponse ToResponse(Broadcast b) =>
        new(b.Id, b.Subject, b.Body, b.Audience.ToString(), b.GradeLevel, b.ScheduledFor, b.CreatedAt);
}
