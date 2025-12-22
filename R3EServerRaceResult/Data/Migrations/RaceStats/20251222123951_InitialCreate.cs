using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace R3EServerRaceResult.Data.Migrations.RaceStats
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "race_stats");

            migrationBuilder.CreateTable(
                name: "drivers",
                schema: "race_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drivers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                schema: "race_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    track_id = table.Column<int>(type: "integer", nullable: false),
                    layout_id = table.Column<int>(type: "integer", nullable: false),
                    event_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    server_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "race_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    session_type = table.Column<string>(type: "text", nullable: false),
                    session_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_events",
                        column: x => x.event_id,
                        principalSchema: "race_stats",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                schema: "race_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    driver_id = table.Column<int>(type: "integer", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    incident_type = table.Column<string>(type: "text", nullable: false),
                    incident_points = table.Column<int>(type: "integer", nullable: false),
                    lap_number = table.Column<int>(type: "integer", nullable: false),
                    involved_driver_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.id);
                    table.ForeignKey(
                        name: "fk_incidents_drivers",
                        column: x => x.driver_id,
                        principalSchema: "race_stats",
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_incidents_involved_drivers",
                        column: x => x.involved_driver_id,
                        principalSchema: "race_stats",
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_incidents_sessions",
                        column: x => x.session_id,
                        principalSchema: "race_stats",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "laps",
                schema: "race_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    driver_id = table.Column<int>(type: "integer", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    lap_number = table.Column<int>(type: "integer", nullable: false),
                    lap_time = table.Column<long>(type: "bigint", nullable: true),
                    sector_1_time = table.Column<long>(type: "bigint", nullable: true),
                    sector_2_time = table.Column<long>(type: "bigint", nullable: true),
                    sector_3_time = table.Column<long>(type: "bigint", nullable: true),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laps", x => x.id);
                    table.ForeignKey(
                        name: "fk_laps_drivers",
                        column: x => x.driver_id,
                        principalSchema: "race_stats",
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_laps_sessions",
                        column: x => x.session_id,
                        principalSchema: "race_stats",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "results",
                schema: "race_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    driver_id = table.Column<int>(type: "integer", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    car_id = table.Column<int>(type: "integer", nullable: false),
                    start_position = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    class_start_position = table.Column<int>(type: "integer", nullable: false),
                    class_position = table.Column<int>(type: "integer", nullable: false),
                    total_race_time = table.Column<long>(type: "bigint", nullable: false),
                    best_lap_time = table.Column<long>(type: "bigint", nullable: true),
                    finish_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_laps = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_results_drivers",
                        column: x => x.driver_id,
                        principalSchema: "race_stats",
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_results_sessions",
                        column: x => x.session_id,
                        principalSchema: "race_stats",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_drivers_name",
                schema: "race_stats",
                table: "drivers",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_events_event_date",
                schema: "race_stats",
                table: "events",
                column: "event_date");

            migrationBuilder.CreateIndex(
                name: "ix_events_layout_id",
                schema: "race_stats",
                table: "events",
                column: "layout_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_track_id",
                schema: "race_stats",
                table: "events",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_driver_id",
                schema: "race_stats",
                table: "incidents",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_incident_type",
                schema: "race_stats",
                table: "incidents",
                column: "incident_type");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_involved_driver_id",
                schema: "race_stats",
                table: "incidents",
                column: "involved_driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_session_id",
                schema: "race_stats",
                table: "incidents",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_laps_driver_id",
                schema: "race_stats",
                table: "laps",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_laps_lap_time",
                schema: "race_stats",
                table: "laps",
                column: "lap_time");

            migrationBuilder.CreateIndex(
                name: "ix_laps_session_driver_lap",
                schema: "race_stats",
                table: "laps",
                columns: new[] { "session_id", "driver_id", "lap_number" });

            migrationBuilder.CreateIndex(
                name: "ix_laps_session_id",
                schema: "race_stats",
                table: "laps",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_results_best_lap_time",
                schema: "race_stats",
                table: "results",
                column: "best_lap_time");

            migrationBuilder.CreateIndex(
                name: "ix_results_car_id",
                schema: "race_stats",
                table: "results",
                column: "car_id");

            migrationBuilder.CreateIndex(
                name: "ix_results_driver_id",
                schema: "race_stats",
                table: "results",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_results_position",
                schema: "race_stats",
                table: "results",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_results_session_id",
                schema: "race_stats",
                table: "results",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_event_id",
                schema: "race_stats",
                table: "sessions",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_event_type_number",
                schema: "race_stats",
                table: "sessions",
                columns: new[] { "event_id", "session_type", "session_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sessions_session_type",
                schema: "race_stats",
                table: "sessions",
                column: "session_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incidents",
                schema: "race_stats");

            migrationBuilder.DropTable(
                name: "laps",
                schema: "race_stats");

            migrationBuilder.DropTable(
                name: "results",
                schema: "race_stats");

            migrationBuilder.DropTable(
                name: "drivers",
                schema: "race_stats");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "race_stats");

            migrationBuilder.DropTable(
                name: "events",
                schema: "race_stats");
        }
    }
}
