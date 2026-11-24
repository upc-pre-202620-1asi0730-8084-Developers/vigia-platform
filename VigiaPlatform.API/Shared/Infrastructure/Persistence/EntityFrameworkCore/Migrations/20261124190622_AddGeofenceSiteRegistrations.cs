using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddGeofenceSiteRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "geofence_site_registrations",
                columns: table => new
                {
                    company_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    site_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    geofence_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_geofence_site_registrations", x => new { x.company_id, x.site_id });
                    table.ForeignKey(
                        name: "fk_geofence_site_registrations_geofences_geofence_id",
                        column: x => x.geofence_id,
                        principalTable: "geofences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_geofence_site_registrations_geofence_id",
                table: "geofence_site_registrations",
                column: "geofence_id");

            migrationBuilder.Sql(@"INSERT INTO geofence_site_registrations (company_id, site_id, geofence_id)
                SELECT TRIM(company_id), TRIM(site_id), MIN(id) FROM geofences
                GROUP BY TRIM(company_id), TRIM(site_id)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "geofence_site_registrations");
        }
    }
}
