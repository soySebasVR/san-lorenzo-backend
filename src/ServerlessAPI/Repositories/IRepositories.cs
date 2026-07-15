using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Repositories;

public interface IUserRepository
{
    /// <summary>Null unless the email, password and active flag all check out.</summary>
    Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default);

    Task<CurrentUser?> GetCurrentAsync(int userId, CancellationToken ct = default);
}

public interface IDashboardRepository
{
    Task<DashboardResponse> GetDashboardAsync(int teacherId, CancellationToken ct = default);
}

public interface ICourseRepository
{
    Task<IReadOnlyList<CourseResponse>> GetCoursesAsync(
        int teacherId, string? section, string? gradeLevel, string? name, CancellationToken ct = default);

    Task<CourseResponse?> GetCourseAsync(int courseId, int teacherId, CancellationToken ct = default);
}

public interface IGradeRepository
{
    Task<GradesResponse> GetGradesAsync(
        int teacherId, string course, string gradeLevel, string section, string term, CancellationToken ct = default);

    Task UpsertGradeAsync(int studentId, int teacherId, UpdateGradeRequest request, CancellationToken ct = default);
}

public interface IAttendanceRepository
{
    Task<AttendanceResponse> GetAttendanceAsync(
        int teacherId, int courseId, DateOnly date, CancellationToken ct = default);

    Task SaveAttendanceAsync(int teacherId, SaveAttendanceRequest request, CancellationToken ct = default);
}

public interface IScheduleRepository
{
    Task<ScheduleResponse> GetScheduleAsync(int teacherId, CancellationToken ct = default);
}

public interface IAnnouncementRepository
{
    Task<AnnouncementResponse> SendAnnouncementAsync(
        int teacherId, SendAnnouncementRequest request, CancellationToken ct = default);
}

public interface IProfileRepository
{
    Task<ProfileResponse?> GetProfileAsync(int teacherId, CancellationToken ct = default);

    Task<ProfileResponse> UpdateProfileAsync(
        int teacherId, UpdateProfileRequest request, CancellationToken ct = default);
}
