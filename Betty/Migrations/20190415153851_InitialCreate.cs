using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Betty.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false),
                    Timezone = table.Column<string>(nullable: false),
                    Repetition = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscordServers",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    Language = table.Column<string>(nullable: false),
                    PublicChannel = table.Column<ulong>(nullable: true),
                    NotificationChannel = table.Column<ulong>(nullable: true),
                    ClockChannel = table.Column<ulong>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordServers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentNotifications",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppointmentId = table.Column<ulong>(nullable: false),
                    Offset = table.Column<TimeSpan>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentNotifications_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    DiscordServerId = table.Column<ulong>(nullable: false),
                    Channel = table.Column<ulong>(nullable: false),
                    Invite = table.Column<string>(nullable: false),
                    Deadline = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.DiscordServerId);
                    table.ForeignKey(
                        name: "FK_Applications_DiscordServers_DiscordServerId",
                        column: x => x.DiscordServerId,
                        principalTable: "DiscordServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscordAppointments",
                columns: table => new
                {
                    DiscordServerId = table.Column<ulong>(nullable: false),
                    AppointmentId = table.Column<ulong>(nullable: false),
                    NotificationChannel = table.Column<ulong>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordAppointments", x => new { x.DiscordServerId, x.AppointmentId });
                    table.ForeignKey(
                        name: "FK_DiscordAppointments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordAppointments_DiscordServers_DiscordServerId",
                        column: x => x.DiscordServerId,
                        principalTable: "DiscordServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordServerId = table.Column<ulong>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Target = table.Column<ulong>(nullable: false),
                    Level = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_DiscordServers_DiscordServerId",
                        column: x => x.DiscordServerId,
                        principalTable: "DiscordServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotifications_AppointmentId",
                table: "AppointmentNotifications",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordAppointments_AppointmentId",
                table: "DiscordAppointments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_DiscordServerId",
                table: "Permissions",
                column: "DiscordServerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "AppointmentNotifications");

            migrationBuilder.DropTable(
                name: "DiscordAppointments");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "DiscordServers");
        }
    }
}
