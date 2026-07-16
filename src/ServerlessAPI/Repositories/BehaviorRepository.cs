using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class BehaviorRepository(SanLorenzoDbContext db) : IBehaviorRepository
{
    [Tracing(SegmentName = "Create behavior report")]
    public async Task<BehaviorReportResponse> CreateAsync(
        CreateBehaviorReportRequest request, CancellationToken ct = default)
    {
        var studentName = await db.Students.AsNoTracking()
            .Where(s => s.Id == request.StudentId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {request.StudentId} does not exist.");

        var report = new BehaviorReport
        {
            StudentId = request.StudentId,
            Date = request.Date,
            Description = request.Description,
        };

        db.BehaviorReports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new BehaviorReportResponse(report.Id, report.StudentId, studentName, report.Date, report.Description);
    }

    [Tracing(SegmentName = "List behavior reports")]
    public async Task<IReadOnlyList<BehaviorReportResponse>> ListAsync(CancellationToken ct = default) =>
        await db.BehaviorReports
            .AsNoTracking()
            .OrderByDescending(b => b.Date)
            .Select(b => new BehaviorReportResponse(
                b.Id, b.StudentId, b.Student.Name, b.Date, b.Description))
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
