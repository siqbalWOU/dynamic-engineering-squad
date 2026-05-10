using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReportUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data repair for pre-existing reports:
            // older environments can contain Reports.UserId values that do not exist in AspNetUsers.
            // Reassign those orphaned rows to the system reporter before enforcing the FK so the
            // migration succeeds without deleting historical report data.
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = N'user-guid-001')
                BEGIN
                    INSERT INTO [AspNetUsers]
                    (
                        [Id],
                        [UserName],
                        [NormalizedUserName],
                        [Email],
                        [NormalizedEmail],
                        [EmailConfirmed],
                        [PasswordHash],
                        [SecurityStamp],
                        [ConcurrencyStamp],
                        [PhoneNumberConfirmed],
                        [TwoFactorEnabled],
                        [LockoutEnabled],
                        [AccessFailedCount],
                        [IsBanned],
                        [AvatarKey],
                        [AvatarUrl],
                        [SelectedDashboardBackgroundKey],
                        [SelectedActivitySummaryBackgroundKey],
                        [SelectedDashboardBorderKey],
                        [SelectedActivitySummaryBorderKey],
                        [BanReason]
                    )
                    VALUES
                    (
                        N'user-guid-001',
                        N'System Reporter',
                        N'SYSTEM REPORTER',
                        N'system-reporter@local.invalid',
                        N'SYSTEM-REPORTER@LOCAL.INVALID',
                        CAST(1 AS bit),
                        NULL,
                        CONVERT(nvarchar(450), NEWID()),
                        CONVERT(nvarchar(450), NEWID()),
                        CAST(0 AS bit),
                        CAST(0 AS bit),
                        CAST(0 AS bit),
                        0,
                        CAST(0 AS bit),
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL
                    );
                END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE [Reports]
                SET [UserId] = N'user-guid-001'
                WHERE [UserId] IS NULL
                   OR LTRIM(RTRIM([UserId])) = N''
                   OR NOT EXISTS
                   (
                       SELECT 1
                       FROM [AspNetUsers] AS [Users]
                       WHERE [Users].[Id] = [Reports].[UserId]
                   );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_UserId",
                table: "Reports",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_UserId",
                table: "Reports");
        }
    }
}
