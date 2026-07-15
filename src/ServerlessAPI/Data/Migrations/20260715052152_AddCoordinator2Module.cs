using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerlessAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoordinator2Module : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoordinatorAnnouncements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoordinatorAnnouncements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoordinatorAnnouncements_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoordinatorProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ManagementArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    AppNotifications = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoordinatorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoordinatorProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoordinatorReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FiltersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedByUserId = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoordinatorReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoordinatorReports_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionalConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    AcademicPeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttendanceToleranceMinutes = table.Column<int>(type: "int", nullable: false),
                    AbsenceAlertPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionalConfigurations", x => x.Id);
                    table.CheckConstraint("CK_InstitutionalConfigurations_AbsenceAlertPercentage", "AbsenceAlertPercentage BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_InstitutionalConfigurations_AcademicYear", "AcademicYear BETWEEN 2000 AND 2100");
                    table.CheckConstraint("CK_InstitutionalConfigurations_AttendanceToleranceMinutes", "AttendanceToleranceMinutes BETWEEN 0 AND 120");
                    table.ForeignKey(
                        name: "FK_InstitutionalConfigurations_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoordinatorAnnouncementRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoordinatorAnnouncementId = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GradeLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Section = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoordinatorAnnouncementRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoordinatorAnnouncementRecipients_CoordinatorAnnouncements_CoordinatorAnnouncementId",
                        column: x => x.CoordinatorAnnouncementId,
                        principalTable: "CoordinatorAnnouncements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoordinatorAnnouncementRecipients_CoordinatorAnnouncementId",
                table: "CoordinatorAnnouncementRecipients",
                column: "CoordinatorAnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinatorAnnouncementRecipients_TargetType",
                table: "CoordinatorAnnouncementRecipients",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinatorAnnouncements_CreatedByUserId",
                table: "CoordinatorAnnouncements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinatorAnnouncements_Status_CreatedAt",
                table: "CoordinatorAnnouncements",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CoordinatorProfiles_UserId",
                table: "CoordinatorProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoordinatorReports_GeneratedByUserId",
                table: "CoordinatorReports",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinatorReports_ReportType_GeneratedAt",
                table: "CoordinatorReports",
                columns: new[] { "ReportType", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionalConfigurations_UpdatedByUserId",
                table: "InstitutionalConfigurations",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoordinatorAnnouncementRecipients");

            migrationBuilder.DropTable(
                name: "CoordinatorProfiles");

            migrationBuilder.DropTable(
                name: "CoordinatorReports");

            migrationBuilder.DropTable(
                name: "InstitutionalConfigurations");

            migrationBuilder.DropTable(
                name: "CoordinatorAnnouncements");
        }
    }
}
