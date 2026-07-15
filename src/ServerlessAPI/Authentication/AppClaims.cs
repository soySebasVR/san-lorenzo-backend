namespace ServerlessAPI.Authentication;

/// <summary>Claims beyond the standard set. Present only when the role matches.</summary>
public static class AppClaims
{
    public const string TeacherId = "teacher_id";
    public const string StudentId = "student_id";
}
