using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedDashboardBackground : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedDashboardBackgroundKey",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedDashboardBackgroundKey",
                table: "AspNetUsers");
        }
    }
}
