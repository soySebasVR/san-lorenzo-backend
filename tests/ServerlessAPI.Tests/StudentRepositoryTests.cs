using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class StudentRepositoryTests : SqliteTestBase
{
    private const int StudentId = 100;
    private const int OtherStudentId = 101;
    private const int MathId = 10;
    private const int ScienceId = 11;

    protected override Task SeedAsync()
    {
        Db.Teachers.AddRange(
            new Teacher { Id = 1, FullName = "Ana Torres", Email = "ana@sl.edu", Position = "Docente" },
            new Teacher { Id = 2, FullName = "Luis Paz", Email = "luis@sl.edu", Position = "Docente" });

        // Two courses of section 3ro A, plus one of another section.
        Db.Courses.AddRange(
            new Course { Id = MathId, TeacherId = 1, Name = "Matemática", GradeLevel = "3ro", Section = "A", Color = "#3B82F6" },
            new Course { Id = ScienceId, TeacherId = 2, Name = "Ciencias", GradeLevel = "3ro", Section = "A", Color = "#10B981" },
            new Course { Id = 12, TeacherId = 1, Name = "Historia", GradeLevel = "4to", Section = "B", Color = "#EF4444" });

        Db.Students.AddRange(
            new Student { Id = StudentId, Name = "Carlos Ruiz", GradeLevel = "3ro", Section = "A" },
            new Student { Id = OtherStudentId, Name = "Marta Díaz", GradeLevel = "4to", Section = "B" });

        // Carlos: graded in Matemática, not in Ciencias.
        Db.Grades.Add(new Grade
        {
            StudentId = StudentId, CourseId = MathId, Term = "2026-I",
            Score1 = 16, Score2 = 16, Score3 = 16, Score4 = 16, Score5 = 16, Average = 16,
        });

        Db.Attendance.AddRange(
            new Attendance { StudentId = StudentId, CourseId = MathId, Date = new DateOnly(2026, 7, 10), Present = true },
            new Attendance { StudentId = StudentId, CourseId = MathId, Date = new DateOnly(2026, 7, 11), Present = false });

        Db.ScheduleSlots.Add(new ScheduleSlot
        {
            Id = 1, CourseId = MathId, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(9, 0),
            DayOfWeek = 1, Icon = "bi-calculator",
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetCourses_returns_every_course_of_the_students_section()
    {
        var courses = await new StudentRepository(Db).GetCoursesAsync(StudentId, Ct);

        Assert.Equal(2, courses.Count); // Matemática + Ciencias, not Historia (4to B)
        Assert.Contains(courses, c => c.Name == "Matemática" && c.TeacherName == "Ana Torres");
        Assert.Contains(courses, c => c.Name == "Ciencias" && c.TeacherName == "Luis Paz");
    }

    [Fact]
    public async Task GetGrades_lists_all_section_courses_ungraded_ones_as_zero()
    {
        var result = await new StudentRepository(Db).GetGradesAsync(StudentId, term: null, Ct);

        Assert.Equal("2026-I", result.Term);
        Assert.Equal(2, result.Courses.Count);
        Assert.Equal(16m, result.Courses.Single(c => c.Name == "Matemática").Average);
        Assert.Equal(0m, result.Courses.Single(c => c.Name == "Ciencias").Average);
    }

    [Fact]
    public async Task GetDashboard_computes_average_and_attendance()
    {
        var dash = await new StudentRepository(Db).GetDashboardAsync(StudentId, Ct);

        Assert.Equal(2, dash.TotalCourses);
        Assert.Equal(16m, dash.OverallAverage);   // only the graded course counts
        Assert.Equal(50, dash.AttendancePercentage); // 1 present of 2 records
    }

    [Fact]
    public async Task GetAttendance_returns_history_newest_first_with_absence_count()
    {
        var att = await new StudentRepository(Db).GetAttendanceAsync(StudentId, Ct);

        Assert.Equal(1, att.TotalAbsences);
        Assert.Equal(2, att.History.Count);
        Assert.Equal(new DateOnly(2026, 7, 11), att.History[0].Date); // newest first
    }

    [Fact]
    public async Task GetSchedule_only_covers_the_students_section()
    {
        var schedule = await new StudentRepository(Db).GetScheduleAsync(StudentId, Ct);

        Assert.Equal("3ro", schedule.GradeLevel);
        var slot = Assert.Single(schedule.Classes);
        Assert.Equal("08:00", slot.StartTime);
        Assert.Equal("Matemática", slot.Name);
    }

    [Fact]
    public async Task UpdateProfile_changes_contact_fields_only()
    {
        var repo = new StudentRepository(Db);

        var updated = await repo.UpdateProfileAsync(StudentId, new UpdateStudentProfileRequest
        {
            Email = "carlos@gmail.com",
            Phone = "999888777",
            EmailNotifications = true,
        }, Ct);

        Assert.Equal("carlos@gmail.com", updated.Email);
        Assert.Equal("Carlos Ruiz", updated.FullName); // name untouched
        Assert.Equal("3ro", updated.GradeLevel);       // section untouched
        Assert.True(updated.EmailNotifications);
    }
}
