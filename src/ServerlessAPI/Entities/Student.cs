namespace ServerlessAPI.Entities;

/// <summary>
/// A student belongs to a grade + section (e.g. "3ro" / "A") and takes every Course of
/// that section. There is no direct student→course link: their courses are the Courses
/// whose GradeLevel and Section match. Grades and Attendance still reference the student
/// per course.
/// </summary>
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;

    // Profile fields the student can edit.
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool EmailNotifications { get; set; }
    public bool AppNotifications { get; set; }

    public ICollection<Grade> Grades { get; set; } = [];
    public ICollection<Attendance> AttendanceRecords { get; set; } = [];
}
