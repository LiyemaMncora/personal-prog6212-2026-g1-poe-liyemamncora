using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    roleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.roleID);
                    table.CheckConstraint("CK_Roles_roleName", "[roleName] IN ('Organiser', 'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleID = table.Column<int>(type: "int", nullable: false),
                    firstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    lastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    passwordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    phoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    dateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    profilePictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.userID);
                    table.ForeignKey(
                        name: "FK_Users_Role",
                        column: x => x.roleID,
                        principalTable: "Roles",
                        principalColumn: "roleID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    eventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organiserID = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    eventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    distanceKM = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    bannerImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.eventID);
                    table.CheckConstraint("CK_Events_distanceKM", "[distanceKM] > 0");
                    table.CheckConstraint("CK_Events_eventType", "[eventType] IN ('Running', 'Walking', 'Cycling')");
                    table.ForeignKey(
                        name: "FK_Events_Organiser",
                        column: x => x.organiserID,
                        principalTable: "Users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    categoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eventID = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    distanceKM = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    entryFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    maxParticipants = table.Column<int>(type: "int", nullable: true),
                    startTime = table.Column<TimeSpan>(type: "time(0)", nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.categoryID);
                    table.CheckConstraint("CK_Categories_distanceKM", "[distanceKM] > 0");
                    table.CheckConstraint("CK_Categories_entryFee", "[entryFee] >= 0");
                    table.ForeignKey(
                        name: "FK_Categories_Event",
                        column: x => x.eventID,
                        principalTable: "Events",
                        principalColumn: "eventID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    enrolmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eventID = table.Column<int>(type: "int", nullable: false),
                    categoryID = table.Column<int>(type: "int", nullable: false),
                    participantID = table.Column<int>(type: "int", nullable: false),
                    raceNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Confirmed"),
                    enrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.enrolmentID);
                    table.CheckConstraint("CK_Enrolments_status", "[status] IN ('Confirmed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Enrolments_Category",
                        column: x => x.categoryID,
                        principalTable: "Categories",
                        principalColumn: "categoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrolments_Event",
                        column: x => x.eventID,
                        principalTable: "Events",
                        principalColumn: "eventID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Participant",
                        column: x => x.participantID,
                        principalTable: "Users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    resultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    enrolmentID = table.Column<int>(type: "int", nullable: false),
                    finishTime = table.Column<TimeSpan>(type: "time(0)", nullable: true),
                    positionOverall = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Finished"),
                    capturedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.resultID);
                    table.CheckConstraint("CK_Results_position", "[positionOverall] IS NULL OR [positionOverall] > 0");
                    table.CheckConstraint("CK_Results_status", "[status] IN ('Finished', 'DNF', 'DNS')");
                    table.ForeignKey(
                        name: "FK_Results_Enrolment",
                        column: x => x.enrolmentID,
                        principalTable: "Enrolments",
                        principalColumn: "enrolmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "roleID", "roleName" },
                values: new object[,]
                {
                    { 1, "Organiser" },
                    { 2, "Participant" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_EventName",
                table: "Categories",
                columns: new[] { "eventID", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_eventID",
                table: "Enrolments",
                column: "eventID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_participantID",
                table: "Enrolments",
                column: "participantID");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_CategoryParticipant",
                table: "Enrolments",
                columns: new[] { "categoryID", "participantID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_raceNumber",
                table: "Enrolments",
                column: "raceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_organiserID",
                table: "Events",
                column: "organiserID");

            migrationBuilder.CreateIndex(
                name: "UQ_Results_enrolment",
                table: "Results",
                column: "enrolmentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_roleName",
                table: "Roles",
                column: "roleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_roleID",
                table: "Users",
                column: "roleID");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_email",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
