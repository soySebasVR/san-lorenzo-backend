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

// ── Student area (scoped to the caller's studentId) ──────────────────────────
public interface IStudentRepository
{
    Task<StudentDashboardResponse> GetDashboardAsync(int studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentCourse>> GetCoursesAsync(int studentId, CancellationToken ct = default);
    Task<StudentGradesResponse> GetGradesAsync(int studentId, string? term, CancellationToken ct = default);
    Task<StudentAttendanceResponse> GetAttendanceAsync(int studentId, CancellationToken ct = default);
    Task<StudentScheduleResponse> GetScheduleAsync(int studentId, CancellationToken ct = default);
    Task<StudentProfileResponse?> GetProfileAsync(int studentId, CancellationToken ct = default);
    Task<StudentProfileResponse> UpdateProfileAsync(
        int studentId, UpdateStudentProfileRequest request, CancellationToken ct = default);
}

// ── Coordinator area ─────────────────────────────────────────────────────────
public interface ICoordinatorDashboardRepository
{
    Task<CoordinatorDashboardResponse> GetDashboardAsync(CancellationToken ct = default);
}

public interface IUserAdminRepository
{
    Task<IReadOnlyList<UserListItem>> ListAsync(string? search, CancellationToken ct = default);
    Task<UserDetail?> GetAsync(int userId, CancellationToken ct = default);
    Task<UserDetail> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDetail?> UpdateAsync(int userId, UpdateUserRequest request, CancellationToken ct = default);
}

public interface ICourseAdminRepository
{
    Task<IReadOnlyList<AdminCourse>> ListAsync(CancellationToken ct = default);
    Task<AdminCourse?> GetAsync(int courseId, CancellationToken ct = default);
    Task<AdminCourse> CreateAsync(SaveCourseRequest request, CancellationToken ct = default);
    Task<AdminCourse?> UpdateAsync(int courseId, SaveCourseRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int courseId, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherOption>> GetTeachersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GradeSection>> GetGradeSectionsAsync(CancellationToken ct = default);
}

public interface IScheduleAdminRepository
{
    Task<IReadOnlyList<AdminScheduleSlot>> ListAsync(CancellationToken ct = default);
    Task<AdminScheduleSlot> CreateAsync(SaveScheduleSlotRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int slotId, CancellationToken ct = default);
}

public interface ISettingsRepository
{
    Task<SettingsResponse> GetAsync(CancellationToken ct = default);
    Task<SettingsResponse> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default);
}

public interface ICoordinatorProfileRepository
{
    Task<CoordinatorProfileResponse?> GetAsync(int userId, CancellationToken ct = default);
    Task<CoordinatorProfileResponse> UpdateAsync(
        int userId, UpdateCoordinatorProfileRequest request, CancellationToken ct = default);
}

// ── Assignments, reports, broadcasts, behavior ───────────────────────────────
public interface IAssignmentRepository
{
    Task<TeacherAssignment> CreateAsync(int teacherId, CreateAssignmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherAssignment>> ListForTeacherAsync(int teacherId, int? courseId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int teacherId, int assignmentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentAssignment>> ListForStudentAsync(int studentId, string? type, CancellationToken ct = default);
}

public interface IReportRepository
{
    Task<ReportListItem> GenerateAsync(GenerateReportRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ReportListItem>> ListAsync(CancellationToken ct = default);
    Task<string?> BuildTextAsync(int reportId, CancellationToken ct = default);
}

public interface IBroadcastRepository
{
    Task<BroadcastResponse> CreateAsync(CreateBroadcastRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<BroadcastResponse>> ListAsync(CancellationToken ct = default);
}

public interface IBehaviorRepository
{
    Task<BehaviorReportResponse> CreateAsync(CreateBehaviorReportRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<BehaviorReportResponse>> ListAsync(CancellationToken ct = default);
}
