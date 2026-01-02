using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace R3EServerRaceResult.Migrations
{
    /// <inheritdoc />
    public partial class InitialRaceStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrackId = table.Column<int>(type: "integer", nullable: false),
                    LayoutId = table.Column<int>(type: "integer", nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    SessionType = table.Column<string>(type: "text", nullable: false),
                    SessionNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    IncidentType = table.Column<string>(type: "text", nullable: false),
                    IncidentPoints = table.Column<int>(type: "integer", nullable: false),
                    LapNumber = table.Column<int>(type: "integer", nullable: false),
                    InvolvedDriverId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incidents_Drivers_InvolvedDriverId",
                        column: x => x.InvolvedDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Incidents_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Laps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    LapNumber = table.Column<int>(type: "integer", nullable: false),
                    LapTime = table.Column<long>(type: "bigint", nullable: true),
                    Sector1Time = table.Column<long>(type: "bigint", nullable: true),
                    Sector2Time = table.Column<long>(type: "bigint", nullable: true),
                    Sector3Time = table.Column<long>(type: "bigint", nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Laps_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Laps_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    StartPosition = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ClassStartPosition = table.Column<int>(type: "integer", nullable: false),
                    ClassPosition = table.Column<int>(type: "integer", nullable: false),
                    TotalRaceTime = table.Column<long>(type: "bigint", nullable: false),
                    BestLapTime = table.Column<long>(type: "bigint", nullable: true),
                    FinishStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalLaps = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Results_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Results_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_Name",
                table: "Drivers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventDate",
                table: "Events",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_Events_LayoutId",
                table: "Events",
                column: "LayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_TrackId",
                table: "Events",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_DriverId",
                table: "Incidents",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IncidentType",
                table: "Incidents",
                column: "IncidentType");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_InvolvedDriverId",
                table: "Incidents",
                column: "InvolvedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_SessionId",
                table: "Incidents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Laps_DriverId",
                table: "Laps",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Laps_LapTime",
                table: "Laps",
                column: "LapTime");

            migrationBuilder.CreateIndex(
                name: "IX_Laps_SessionId",
                table: "Laps",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Laps_SessionId_DriverId_LapNumber",
                table: "Laps",
                columns: new[] { "SessionId", "DriverId", "LapNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Results_BestLapTime",
                table: "Results",
                column: "BestLapTime");

            migrationBuilder.CreateIndex(
                name: "IX_Results_CarId",
                table: "Results",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_DriverId",
                table: "Results",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_Position",
                table: "Results",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_Results_SessionId",
                table: "Results",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EventId",
                table: "Sessions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SessionType",
                table: "Sessions",
                column: "SessionType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Laps");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
