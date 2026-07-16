using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Repositories;

public sealed class AcademicManagementRepository(SanLorenzoDbContext db) : IAcademicManagementRepository
{
    public async Task<IReadOnlyList<CoordinatorCourseResponse>> GetCoursesAsync(CancellationToken ct = default)
    {
        return await db.Courses
            .Include(c => c.AcademicGrade)
            .Include(c => c.SectionEntity)
            .Include(c => c.Teacher)
            .Select(c => new CoordinatorCourseResponse(
                c.Id,
                c.Name,
                c.AcademicGrade != null ? c.AcademicGrade.Name : "N/A",
                c.SectionEntity != null ? c.SectionEntity.Name : "N/A",
                c.Teacher != null ? c.Teacher.Id.ToString() : "N/A",
                c.WeeklyHours))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<CoordinatorCourseResponse> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        var course = new Course
        {
            Name = request.Name,
            AcademicGradeId = request.GradeId,
            SectionId = request.SectionId,
            TeacherId = request.TeacherId,
            WeeklyHours = request.WeeklyHours
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);

        return new CoordinatorCourseResponse(course.Id, course.Name, "N/A", "N/A", "N/A", course.WeeklyHours);
    }

    public async Task<CoordinatorCourseResponse> UpdateCourseAsync(int id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        
        if (course == null) 
        {
            throw new Exception("Course not found.");
        }

        course.Name = request.Name;
        course.AcademicGradeId = request.GradeId;
        course.SectionId = request.SectionId;
        course.TeacherId = request.TeacherId;
        course.WeeklyHours = request.WeeklyHours;

        await db.SaveChangesAsync(ct);

        return new CoordinatorCourseResponse(course.Id, course.Name, "N/A", "N/A", "N/A", course.WeeklyHours);
    }
}