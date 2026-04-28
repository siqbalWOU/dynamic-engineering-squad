using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedDashboardBorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedDashboardBorderKey",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedDashboardBorderKey",
                table: "AspNetUsers");
        }
    }
}
