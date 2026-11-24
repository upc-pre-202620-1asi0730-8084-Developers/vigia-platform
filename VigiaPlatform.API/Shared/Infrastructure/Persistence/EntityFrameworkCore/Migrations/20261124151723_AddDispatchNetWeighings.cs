using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchNetWeighings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "dispatches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "dispatch_weighings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    dispatch_id = table.Column<int>(type: "int", nullable: false),
                    weight_kg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    expected_weight_kg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    tolerance_kg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    comparison_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    vehicle_id = table.Column<int>(type: "int", nullable: false),
                    vehicle_plate = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    cargo_fingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    weighed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    recorded_by_account_id = table.Column<int>(type: "int", nullable: false),
                    recorded_by = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    request_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispatch_weighings", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispatch_weighings_dispatches_dispatch_id",
                        column: x => x.dispatch_id,
                        principalTable: "dispatches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_weighings_dispatch_id_request_id",
                table: "dispatch_weighings",
                columns: new[] { "dispatch_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dispatch_weighings");

            migrationBuilder.DropColumn(
                name: "version",
                table: "dispatches");
        }
    }
}
