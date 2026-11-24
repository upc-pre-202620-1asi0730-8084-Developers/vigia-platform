using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchTripsAndPositionOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "position_reported_at",
                table: "vehicles",
                type: "datetime(6)",
                precision: 6,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "position_version",
                table: "vehicles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "notified_at",
                table: "transit_alerts",
                type: "datetime(6)",
                precision: 6,
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "detected_at",
                table: "transit_alerts",
                type: "datetime(6)",
                precision: 6,
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "reported_at",
                table: "telemetry_positions",
                type: "datetime(6)",
                precision: 6,
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "started_at",
                table: "routes",
                type: "datetime(6)",
                precision: 6,
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "estimated_arrival",
                table: "routes",
                type: "datetime(6)",
                precision: 6,
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "arrived_at",
                table: "routes",
                type: "datetime(6)",
                precision: 6,
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "routes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "dispatch_trips",
                columns: table => new
                {
                    dispatch_id = table.Column<int>(type: "int", nullable: false),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    vehicle_id = table.Column<int>(type: "int", nullable: false),
                    active_vehicle_id = table.Column<int>(type: "int", nullable: true),
                    gps_device_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    origin_geofence_id = table.Column<int>(type: "int", nullable: false),
                    request_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    request_fingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    departed_at = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    started_by = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    ended_at = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    end_reason = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispatch_trips", x => x.dispatch_id);
                    table.ForeignKey(
                        name: "fk_dispatch_trips_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_trips_active_vehicle_id",
                table: "dispatch_trips",
                column: "active_vehicle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_trips_route_id",
                table: "dispatch_trips",
                column: "route_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dispatch_trips");

            migrationBuilder.DropColumn(
                name: "position_reported_at",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "position_version",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "version",
                table: "routes");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "notified_at",
                table: "transit_alerts",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldPrecision: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "detected_at",
                table: "transit_alerts",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldPrecision: 6);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "reported_at",
                table: "telemetry_positions",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldPrecision: 6);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "started_at",
                table: "routes",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldPrecision: 6);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "estimated_arrival",
                table: "routes",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldPrecision: 6);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "arrived_at",
                table: "routes",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldPrecision: 6,
                oldNullable: true);
        }
    }
}
