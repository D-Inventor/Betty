using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Betty.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    GuildID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Language = table.Column<string>(nullable: true),
                    Public = table.Column<ulong>(nullable: true),
                    Notification = table.Column<ulong>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.GuildID);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    GuildID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FK_Application_Guild = table.Column<ulong>(nullable: false),
                    Channel = table.Column<ulong>(nullable: false),
                    InviteID = table.Column<string>(nullable: true),
                    Deadline = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.GuildID);
                    table.ForeignKey(
                        name: "FK_Applications_Guilds_FK_Application_Guild",
                        column: x => x.FK_Application_Guild,
                        principalTable: "Guilds",
                        principalColumn: "GuildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FK_Event_Guild = table.Column<ulong>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    NotificationChannel = table.Column<ulong>(nullable: false),
                    DoNotifications = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventID);
                    table.ForeignKey(
                        name: "FK_Events_Guilds_FK_Event_Guild",
                        column: x => x.FK_Event_Guild,
                        principalTable: "Guilds",
                        principalColumn: "GuildID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventNotifications",
                columns: table => new
                {
                    NotificationID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FK_EventNotification_Event = table.Column<ulong>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false),
                    ResponseKeyword = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventNotifications", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_EventNotifications_Events_FK_EventNotification_Event",
                        column: x => x.FK_EventNotification_Event,
                        principalTable: "Events",
                        principalColumn: "EventID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_FK_Application_Guild",
                table: "Applications",
                column: "FK_Application_Guild",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventNotifications_FK_EventNotification_Event",
                table: "EventNotifications",
                column: "FK_EventNotification_Event");

            migrationBuilder.CreateIndex(
                name: "IX_Events_FK_Event_Guild",
                table: "Events",
                column: "FK_Event_Guild");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "EventNotifications");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
