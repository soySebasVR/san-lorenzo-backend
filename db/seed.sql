-- Development seed data. Run after db/schema.sql. Idempotent.
--
-- PasswordHash values are real PBKDF2 hashes produced by the same PasswordHasher the API
-- uses. Plaintext passwords (DEVELOPMENT ONLY — do not ship these):
--
--   teacher@sanlorenzo.edu.pe      Docente#2026
--   student@sanlorenzo.edu.pe      Alumno#2026
--   coordinator@sanlorenzo.edu.pe  Coord#2026

SET NOCOUNT ON;

-- ── Teachers ──────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Teachers WHERE Email = 'teacher@sanlorenzo.edu.pe')
    INSERT INTO Teachers (FullName, Email, Position, Subjects, EmailNotifications, AppNotifications)
    VALUES ('Ana Torres Quispe', 'teacher@sanlorenzo.edu.pe', 'Docente', 'Matemática, Física', 1, 0);

IF NOT EXISTS (SELECT 1 FROM Teachers WHERE Email = 'jsalas@sanlorenzo.edu.pe')
    INSERT INTO Teachers (FullName, Email, Position, Subjects, EmailNotifications, AppNotifications)
    VALUES ('Jorge Salas Vega', 'jsalas@sanlorenzo.edu.pe', 'Docente', 'Ciencias, Historia', 1, 0);

DECLARE @Ana INT   = (SELECT Id FROM Teachers WHERE Email = 'teacher@sanlorenzo.edu.pe');
DECLARE @Jorge INT = (SELECT Id FROM Teachers WHERE Email = 'jsalas@sanlorenzo.edu.pe');

-- ── Courses: section 3ro A has several courses, taught by different teachers ─────
IF NOT EXISTS (SELECT 1 FROM Courses WHERE GradeLevel = '3ro' AND Section = 'A')
    INSERT INTO Courses (TeacherId, Name, GradeLevel, Section, Color, ScheduleText) VALUES
        (@Ana,   'Matemática', '3ro', 'A', '#3B82F6', 'Lun y Mié 08:00-09:00'),
        (@Jorge, 'Ciencias',   '3ro', 'A', '#10B981', 'Mar y Jue 09:00-10:00'),
        (@Jorge, 'Historia',   '3ro', 'A', '#EF4444', 'Vie 10:00-11:00'),
        (@Ana,   'Física',     '4to', 'A', '#8B5CF6', 'Vie 09:00-11:00');

DECLARE @Math INT    = (SELECT Id FROM Courses WHERE Name = 'Matemática' AND GradeLevel = '3ro' AND Section = 'A');
DECLARE @Science INT = (SELECT Id FROM Courses WHERE Name = 'Ciencias'   AND GradeLevel = '3ro' AND Section = 'A');

-- ── Students of 3ro A ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Students WHERE GradeLevel = '3ro' AND Section = 'A')
    INSERT INTO Students (Name, GradeLevel, Section, Email, Phone, EmailNotifications, AppNotifications) VALUES
        ('Carlos Ruiz Mendoza',  '3ro', 'A', NULL, NULL, 0, 0),
        ('Lucía Fernández Paz',  '3ro', 'A', NULL, NULL, 0, 0),
        ('Miguel Ángel Soto',    '3ro', 'A', NULL, NULL, 0, 0),
        ('Valeria Chávez Ríos',  '3ro', 'A', NULL, NULL, 0, 0);

DECLARE @Carlos INT = (SELECT TOP 1 Id FROM Students WHERE GradeLevel = '3ro' AND Section = 'A' ORDER BY Id);

-- ── Grades for Carlos (term 2026-I) ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Grades WHERE StudentId = @Carlos)
    INSERT INTO Grades (StudentId, CourseId, Term, Score1, Score2, Score3, Score4, Score5, Average) VALUES
        (@Carlos, @Math,    '2026-I', 16, 15, 17, 18, 14, 16.00),
        (@Carlos, @Science, '2026-I', 14, 13, 15, 16, 12, 14.00);

-- ── Attendance for Carlos ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Attendance WHERE StudentId = @Carlos)
    INSERT INTO Attendance (StudentId, CourseId, Date, Present) VALUES
        (@Carlos, @Math,    '2026-07-07', 1),
        (@Carlos, @Math,    '2026-07-09', 0),
        (@Carlos, @Science, '2026-07-08', 1);

-- ── Weekly schedule for 3ro A ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScheduleSlots WHERE CourseId = @Math)
    -- DayOfWeek: 0 = Sunday … 6 = Saturday, matching Date.getDay() in the frontend.
    INSERT INTO ScheduleSlots (CourseId, StartTime, EndTime, DayOfWeek, Icon) VALUES
        (@Math,    '08:00', '09:00', 1, 'bi-calculator'),
        (@Math,    '08:00', '09:00', 3, 'bi-calculator'),
        (@Science, '09:00', '10:00', 2, 'bi-eyedropper');

-- ── Assignments for 3ro A ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Assignments WHERE CourseId = @Math)
    INSERT INTO Assignments (CourseId, Title, Type, StartDate, DueDate) VALUES
        (@Math,    'Práctica de ecuaciones', 'Task', '2026-07-01', '2026-07-08'),
        (@Math,    'Examen parcial',         'Exam', '2026-07-14', '2026-07-14'),
        (@Science, 'Informe de laboratorio', 'Task', '2026-07-05', '2026-07-12');

-- ── One account per role ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'teacher@sanlorenzo.edu.pe')
    INSERT INTO Users (Email, FullName, PasswordHash, Role, IsActive, EmailNotifications, AppNotifications, TeacherId, StudentId)
    VALUES (
        'teacher@sanlorenzo.edu.pe', 'Ana Torres Quispe',
        'AQAAAAIAAYagAAAAEAp2gatiNLp0XyWMWKJyKvAkAt3y+unDk2I4T7k0oX6nc9+YgdeMKDN3oUcOEpRFpg==',
        'Teacher', 1, 0, 0, @Ana, NULL);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'student@sanlorenzo.edu.pe')
    INSERT INTO Users (Email, FullName, PasswordHash, Role, IsActive, EmailNotifications, AppNotifications, TeacherId, StudentId)
    VALUES (
        'student@sanlorenzo.edu.pe', 'Carlos Ruiz Mendoza',
        'AQAAAAIAAYagAAAAENA80tFWX3l6zjpbrnyzsyCRP9ZGORbfJIG3N6dRWtluFt4WzpsrkLn1RN3om53kmw==',
        'Student', 1, 0, 0, NULL, @Carlos);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'coordinator@sanlorenzo.edu.pe')
    -- Coordinators have no academic profile; CK_Users_ProfileMatchesRole enforces that.
    INSERT INTO Users (Email, FullName, PasswordHash, Role, IsActive, EmailNotifications, AppNotifications, TeacherId, StudentId)
    VALUES (
        'coordinator@sanlorenzo.edu.pe', 'Luis Paz Herrera',
        'AQAAAAIAAYagAAAAEHhqfeHZH2+Il0Ph7yAsOUynPfhifpZ4bBlZ46a7vor/FvAsVj/G59KUuQVJxDba5A==',
        'Coordinator', 1, 0, 0, NULL, NULL);

SELECT Email, Role, IsActive, TeacherId, StudentId FROM Users;
