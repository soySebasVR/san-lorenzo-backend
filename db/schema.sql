IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Broadcasts] (
        [Id] int NOT NULL IDENTITY,
        [Subject] nvarchar(200) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Audience] nvarchar(20) NOT NULL,
        [GradeLevel] nvarchar(20) NULL,
        [ScheduledFor] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Broadcasts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Students] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [GradeLevel] nvarchar(20) NOT NULL,
        [Section] nvarchar(20) NOT NULL,
        [Email] nvarchar(255) NULL,
        [Phone] nvarchar(30) NULL,
        [EmailNotifications] bit NOT NULL,
        [AppNotifications] bit NOT NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [Id] int NOT NULL IDENTITY,
        [SchoolName] nvarchar(150) NOT NULL,
        [AcademicYear] int NOT NULL,
        [CurrentTerm] nvarchar(20) NOT NULL,
        [UnjustifiedAbsenceThreshold] int NOT NULL,
        [LatenessToleranceMinutes] int NOT NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Teachers] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Position] nvarchar(50) NOT NULL,
        [Subjects] nvarchar(255) NULL,
        [EmailNotifications] bit NOT NULL,
        [AppNotifications] bit NOT NULL,
        CONSTRAINT [PK_Teachers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [BehaviorReports] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [Date] date NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_BehaviorReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BehaviorReports_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Courses] (
        [Id] int NOT NULL IDENTITY,
        [TeacherId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [GradeLevel] nvarchar(20) NOT NULL,
        [Section] nvarchar(20) NOT NULL,
        [Color] nvarchar(20) NOT NULL,
        [ScheduleText] nvarchar(100) NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Courses_Teachers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Teachers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Reports] (
        [Id] int NOT NULL IDENTITY,
        [GradeLevel] nvarchar(20) NOT NULL,
        [Term] nvarchar(20) NOT NULL,
        [TeacherId] int NULL,
        [GeneratedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reports_Teachers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Teachers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(255) NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(255) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [EmailNotifications] bit NOT NULL,
        [AppNotifications] bit NOT NULL,
        [TeacherId] int NULL,
        [StudentId] int NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Users_ProfileMatchesRole] CHECK ((Role = 'Teacher'     AND TeacherId IS NOT NULL AND StudentId IS NULL)
    OR (Role = 'Student'     AND StudentId IS NOT NULL AND TeacherId IS NULL)
    OR (Role = 'Coordinator' AND TeacherId IS NULL     AND StudentId IS NULL)),
        CONSTRAINT [FK_Users_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Users_Teachers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Teachers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Announcements] (
        [Id] int NOT NULL IDENTITY,
        [TeacherId] int NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Section] nvarchar(20) NULL,
        [GradeLevel] nvarchar(20) NULL,
        [CourseId] int NULL,
        [RecipientCount] int NOT NULL,
        [SentAt] datetime2 NOT NULL DEFAULT (GETDATE()),
        CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Announcements_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Announcements_Teachers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Teachers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Assignments] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Type] nvarchar(10) NOT NULL,
        [StartDate] date NOT NULL,
        [DueDate] date NOT NULL,
        CONSTRAINT [PK_Assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Assignments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Attendance] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [CourseId] int NOT NULL,
        [Date] date NOT NULL,
        [Present] bit NOT NULL,
        CONSTRAINT [PK_Attendance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Attendance_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Attendance_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [Grades] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [CourseId] int NOT NULL,
        [Term] nvarchar(20) NOT NULL,
        [Score1] decimal(4,2) NOT NULL,
        [Score2] decimal(4,2) NOT NULL,
        [Score3] decimal(4,2) NOT NULL,
        [Score4] decimal(4,2) NOT NULL,
        [Score5] decimal(4,2) NOT NULL,
        [Average] decimal(4,2) NOT NULL,
        CONSTRAINT [PK_Grades] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Grades_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Grades_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE TABLE [ScheduleSlots] (
        [Id] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [DayOfWeek] int NOT NULL,
        [Icon] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_ScheduleSlots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScheduleSlots_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Announcements_CourseId] ON [Announcements] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Announcements_TeacherId] ON [Announcements] ([TeacherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Assignments_CourseId] ON [Assignments] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Attendance_CourseId_Date] ON [Attendance] ([CourseId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Attendance_StudentId_CourseId_Date] ON [Attendance] ([StudentId], [CourseId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BehaviorReports_Date] ON [BehaviorReports] ([Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BehaviorReports_StudentId] ON [BehaviorReports] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Courses_TeacherId] ON [Courses] ([TeacherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Grades_CourseId] ON [Grades] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Grades_StudentId_CourseId_Term] ON [Grades] ([StudentId], [CourseId], [Term]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reports_TeacherId] ON [Reports] ([TeacherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ScheduleSlots_CourseId] ON [ScheduleSlots] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Students_GradeLevel_Section] ON [Students] ([GradeLevel], [Section]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Teachers_Email] ON [Teachers] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_StudentId] ON [Users] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_TeacherId] ON [Users] ([TeacherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716014020_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716014020_InitialCreate', N'10.0.9');
END;

COMMIT;
GO

