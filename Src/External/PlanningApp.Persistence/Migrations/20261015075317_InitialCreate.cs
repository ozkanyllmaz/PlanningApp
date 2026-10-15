using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planning_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    focus_color = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<double>(type: "double precision", nullable: false),
                    assigned_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reminder_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planning_tasks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_planning_tasks_user_id",
                table: "planning_tasks",
                column: "user_id",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_planning_tasks_user_id_assigned_date",
                table: "planning_tasks",
                columns: new[] { "user_id", "assigned_date" },
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planning_tasks");
        }
    }
}
