using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace R3EServerRaceResult.Data.Migrations.R3EContent
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "r3e_content");

            migrationBuilder.CreateTable(
                name: "car_classes",
                schema: "r3e_content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_classes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                schema: "r3e_content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                schema: "r3e_content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cars",
                schema: "r3e_content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    class_id = table.Column<int>(type: "integer", nullable: false),
                    manufacturer_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cars", x => x.id);
                    table.ForeignKey(
                        name: "fk_cars_car_classes",
                        column: x => x.class_id,
                        principalSchema: "r3e_content",
                        principalTable: "car_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cars_manufacturers",
                        column: x => x.manufacturer_id,
                        principalSchema: "r3e_content",
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "layouts",
                schema: "r3e_content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    max_vehicles = table.Column<int>(type: "integer", nullable: false),
                    track_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_layouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_layouts_tracks",
                        column: x => x.track_id,
                        principalSchema: "r3e_content",
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "liveries",
                schema: "r3e_content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    car_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_liveries_cars",
                        column: x => x.car_id,
                        principalSchema: "r3e_content",
                        principalTable: "cars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_car_classes_name",
                schema: "r3e_content",
                table: "car_classes",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_cars_class_id",
                schema: "r3e_content",
                table: "cars",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "ix_cars_id",
                schema: "r3e_content",
                table: "cars",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_cars_manufacturer_id",
                schema: "r3e_content",
                table: "cars",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_cars_name",
                schema: "r3e_content",
                table: "cars",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_layouts_track_id",
                schema: "r3e_content",
                table: "layouts",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "ix_liveries_car_id",
                schema: "r3e_content",
                table: "liveries",
                column: "car_id");

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_name",
                schema: "r3e_content",
                table: "manufacturers",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_tracks_name",
                schema: "r3e_content",
                table: "tracks",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "layouts",
                schema: "r3e_content");

            migrationBuilder.DropTable(
                name: "liveries",
                schema: "r3e_content");

            migrationBuilder.DropTable(
                name: "tracks",
                schema: "r3e_content");

            migrationBuilder.DropTable(
                name: "cars",
                schema: "r3e_content");

            migrationBuilder.DropTable(
                name: "car_classes",
                schema: "r3e_content");

            migrationBuilder.DropTable(
                name: "manufacturers",
                schema: "r3e_content");
        }
    }
}
