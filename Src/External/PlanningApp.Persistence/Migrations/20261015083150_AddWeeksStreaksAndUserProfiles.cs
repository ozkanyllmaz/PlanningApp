using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeksStreaksAndUserProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planning_weeks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    completion_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planning_weeks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "streaks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    longest_streak = table.Column<int>(type: "integer", nullable: false),
                    last_evaluated_date = table.Column<DateOnly>(type: "date", nullable: true),
                    available_freezes = table.Column<int>(type: "integer", nullable: false),
                    consecutive_empty_days = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streaks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_planning_weeks_user_id_week_start_date",
                table: "planning_weeks",
                columns: new[] { "user_id", "week_start_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_streaks_user_id",
                table: "streaks",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planning_weeks");

            migrationBuilder.DropTable(
                name: "streaks");

            migrationBuilder.DropTable(
                name: "user_profiles");
        }
    }
}
