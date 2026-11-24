using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchDeliveryGuides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dispatch_delivery_guides",
                columns: table => new
                {
                    dispatch_id = table.Column<int>(type: "int", nullable: false),
                    guide_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    project_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    site_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    address = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    related_order_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    driver_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    carrier_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    vehicle_id = table.Column<int>(type: "int", nullable: false),
                    vehicle_plate = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    weighing_id = table.Column<int>(type: "int", nullable: false),
                    weight_kg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    expected_weight_kg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    tolerance_kg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    cargo_fingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    items_json = table.Column<string>(type: "longtext", nullable: false),
                    issued_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    issued_by_account_id = table.Column<int>(type: "int", nullable: false),
                    issued_by = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    request_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispatch_delivery_guides", x => x.dispatch_id);
                    table.ForeignKey(
                        name: "fk_dispatch_delivery_guides_dispatches_dispatch_id",
                        column: x => x.dispatch_id,
                        principalTable: "dispatches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_delivery_guides_guide_number",
                table: "dispatch_delivery_guides",
                column: "guide_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dispatch_delivery_guides");
        }
    }
}
