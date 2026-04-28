using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureApp.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDismissed",
                table: "ReportFlags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ModerationActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModeratorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReportIssueId = table.Column<int>(type: "int", nullable: true),
                    TargetContentSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationActionLogs_AspNetUsers_ModeratorId",
                        column: x => x.ModeratorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationActionLogs_ModeratorId",
                table: "ModerationActionLogs",
                column: "ModeratorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModerationActionLogs");

            migrationBuilder.DropColumn(
                name: "IsDismissed",
                table: "ReportFlags");
        }
    }
}
