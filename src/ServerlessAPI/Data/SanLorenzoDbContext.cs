using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Data;

public class SanLorenzoDbContext(DbContextOptions<SanLorenzoDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Attendance> Attendance => Set<Attendance>();
    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();

            // Stored as text: an int column would silently reassign everyone's
            // permissions if the enum were ever reordered.
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

            e.HasIndex(u => u.Email).IsUnique();

            e.HasOne(u => u.Teacher).WithMany()
                .HasForeignKey(u => u.TeacherId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(u => u.Student).WithMany()
                .HasForeignKey(u => u.StudentId).OnDelete(DeleteBehavior.Restrict);

            // The database itself rejects incoherent accounts (a teacher with no TeacherId,
            // a student also pointing at a teacher).
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Users_ProfileMatchesRole",
                """
                (Role = 'Teacher'     AND TeacherId IS NOT NULL AND StudentId IS NULL)
                OR (Role = 'Student'     AND StudentId IS NOT NULL AND TeacherId IS NULL)
                OR (Role = 'Coordinator' AND TeacherId IS NULL     AND StudentId IS NULL)
                """));
        });

        modelBuilder.Entity<Teacher>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.FullName).HasMaxLength(100).IsRequired();
            e.Property(t => t.Email).HasMaxLength(255).IsRequired();
            e.Property(t => t.Position).HasMaxLength(50).IsRequired();
            e.Property(t => t.Subjects).HasMaxLength(255);
            e.HasIndex(t => t.Email).IsUnique();
        });

        modelBuilder.Entity<Course>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.GradeLevel).HasMaxLength(20).IsRequired();
            e.Property(c => c.Section).HasMaxLength(20).IsRequired();
            e.Property(c => c.Color).HasMaxLength(20).IsRequired();
            e.Property(c => c.ScheduleText).HasMaxLength(100);

            e.HasOne(c => c.Teacher).WithMany(t => t.Courses)
                .HasForeignKey(c => c.TeacherId).OnDelete(DeleteBehavior.Restrict);

            // Nearly every query filters by teacher.
            e.HasIndex(c => c.TeacherId);
        });

        modelBuilder.Entity<Student>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();

            e.HasOne(s => s.Course).WithMany(c => c.Students)
                .HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(s => s.CourseId);
        });

        modelBuilder.Entity<Grade>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Term).HasMaxLength(20).IsRequired();

            // 20.00 fits in decimal(4,2); without this EF warns about truncation.
            foreach (var score in new[]
                     {
                         nameof(Grade.Score1), nameof(Grade.Score2), nameof(Grade.Score3),
                         nameof(Grade.Score4), nameof(Grade.Score5), nameof(Grade.Average),
                     })
            {
                e.Property(score).HasColumnType("decimal(4,2)");
            }

            e.HasOne(g => g.Student).WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(g => g.Course).WithMany(c => c.Grades)
                .HasForeignKey(g => g.CourseId).OnDelete(DeleteBehavior.Restrict);

            // Natural key of the upsert; the unique index makes it atomic in the engine.
            e.HasIndex(g => new { g.StudentId, g.CourseId, g.Term }).IsUnique();
        });

        modelBuilder.Entity<Attendance>(e =>
        {
            e.ToTable("Attendance");
            e.HasKey(a => a.Id);
            e.Property(a => a.Date).HasColumnType("date");

            e.HasOne(a => a.Student).WithMany(s => s.AttendanceRecords)
                .HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Course).WithMany(c => c.AttendanceRecords)
                .HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(a => new { a.StudentId, a.CourseId, a.Date }).IsUnique();

            // The dashboard asks "which courses have no attendance today?" on every load.
            e.HasIndex(a => new { a.CourseId, a.Date });
        });

        modelBuilder.Entity<ScheduleSlot>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Icon).HasMaxLength(50).IsRequired();
            e.Property(s => s.StartTime).HasColumnType("time");
            e.Property(s => s.EndTime).HasColumnType("time");

            e.HasOne(s => s.Course).WithMany(c => c.ScheduleSlots)
                .HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(s => s.CourseId);
        });

        modelBuilder.Entity<Announcement>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Subject).HasMaxLength(200).IsRequired();
            e.Property(a => a.Message).IsRequired();
            e.Property(a => a.Section).HasMaxLength(20);
            e.Property(a => a.GradeLevel).HasMaxLength(20);
            e.Property(a => a.SentAt).HasDefaultValueSql("GETDATE()");

            e.HasOne(a => a.Teacher).WithMany(t => t.Announcements)
                .HasForeignKey(a => a.TeacherId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Course).WithMany()
                .HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(a => a.TeacherId);
        });
    }
}
