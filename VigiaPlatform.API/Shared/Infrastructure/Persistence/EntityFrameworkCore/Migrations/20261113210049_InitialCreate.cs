using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "discrepancy_cases",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    dispatch_id = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    reception_id = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    case_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    determined_liability = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    resolution_summary = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discrepancy_cases", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dispatches",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    project_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    site_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    address = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    departure_date = table.Column<DateTime>(type: "date", nullable: false),
                    estimated_time = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    related_order_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    dispatch_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    creation_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    internal_reference = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    observations = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    vehicle_id = table.Column<int>(type: "int", nullable: true),
                    vehicle_plate = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispatches", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "geofences",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    company_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    site_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    center_latitude = table.Column<double>(type: "double", nullable: false),
                    center_longitude = table.Column<double>(type: "double", nullable: false),
                    radius_meters = table.Column<double>(type: "double", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_geofences", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    project_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    requester_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    date = table.Column<DateTime>(type: "date", nullable: false),
                    main_material = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    observations = table.Column<string>(type: "varchar(350)", maxLength: 350, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "receptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    dispatch_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    carrier_plate = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    driver_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    site_id = table.Column<int>(type: "int", nullable: false),
                    site_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    site_resident_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    arrival_registered_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    scale_gross_weight_kg = table.Column<double>(type: "double", nullable: true),
                    scale_tare_weight_kg = table.Column<double>(type: "double", nullable: true),
                    scale_weighing_slip_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    scale_weighed_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    checklist_seal_intact = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    checklist_delivery_note_ok = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    checklist_scale_calibrated = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    checklist_remarks = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    signature_data = table.Column<string>(type: "longtext", nullable: true),
                    signature_signed_by = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    signature_signed_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    discrepancy_reason = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    discrepancy_description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    discrepancy_severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    discrepancy_reported_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    delivery_note_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    receiver_notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receptions", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    dispatch_id = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    vehicle_id = table.Column<int>(type: "int", nullable: false),
                    vehicle_plate = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    origin_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    destination_geofence_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    estimated_arrival = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    arrived_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    deviation_threshold_meters = table.Column<double>(type: "double", nullable: false),
                    stop_threshold_minutes = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    destination_latitude = table.Column<double>(type: "double", nullable: false),
                    destination_longitude = table.Column<double>(type: "double", nullable: false),
                    destination_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    destination_radius_meters = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routes", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    company_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    license_plate = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                    brand = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    max_capacity_tons = table.Column<double>(type: "double", nullable: false),
                    gps_device_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    current_pos_latitude = table.Column<double>(type: "double", nullable: true),
                    current_pos_longitude = table.Column<double>(type: "double", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "discrepancy_case_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    discrepancy_case_id = table.Column<int>(type: "int", nullable: false),
                    stage = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "varchar(1200)", maxLength: 1200, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discrepancy_case_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_discrepancy_case_events_discrepancy_cases_discrepancy_case_id",
                        column: x => x.discrepancy_case_id,
                        principalTable: "discrepancy_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dispatch_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    dispatch_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    material = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    observation = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispatch_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispatch_items_dispatches_dispatch_id",
                        column: x => x.dispatch_id,
                        principalTable: "dispatches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dispatch_timeline_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    dispatch_id = table.Column<int>(type: "int", nullable: false),
                    event_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    user = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    event_date = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispatch_timeline_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispatch_timeline_events_dispatches_dispatch_id",
                        column: x => x.dispatch_id,
                        principalTable: "dispatches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "order_attached_files",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_attached_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_attached_files_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    material = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    observation = table.Column<string>(type: "varchar(350)", maxLength: 350, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "order_timeline_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    user = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    event_date = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_timeline_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_timeline_events_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reception_evidences",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    reception_id = table.Column<int>(type: "int", nullable: false),
                    evidence_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    file_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    attached_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reception_evidences", x => x.id);
                    table.ForeignKey(
                        name: "fk_reception_evidences_receptions_reception_id",
                        column: x => x.reception_id,
                        principalTable: "receptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reception_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    reception_id = table.Column<int>(type: "int", nullable: false),
                    material_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    material_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    dispatched_quantity = table.Column<double>(type: "double", nullable: false),
                    received_quantity = table.Column<double>(type: "double", nullable: false),
                    is_verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    comments = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reception_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_reception_items_receptions_reception_id",
                        column: x => x.reception_id,
                        principalTable: "receptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "route_waypoints",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    latitude = table.Column<double>(type: "double", nullable: false),
                    longitude = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_route_waypoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_route_waypoints_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "telemetry_positions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    latitude = table.Column<double>(type: "double", nullable: false),
                    longitude = table.Column<double>(type: "double", nullable: false),
                    speed_kmh = table.Column<double>(type: "double", nullable: false),
                    reported_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telemetry_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_telemetry_positions_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "transit_alerts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    detected_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    latitude = table.Column<double>(type: "double", nullable: true),
                    longitude = table.Column<double>(type: "double", nullable: true),
                    value = table.Column<double>(type: "double", nullable: true),
                    notified_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transit_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_transit_alerts_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_discrepancy_case_events_discrepancy_case_id",
                table: "discrepancy_case_events",
                column: "discrepancy_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_discrepancy_cases_case_number",
                table: "discrepancy_cases",
                column: "case_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_items_dispatch_id",
                table: "dispatch_items",
                column: "dispatch_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_timeline_events_dispatch_id",
                table: "dispatch_timeline_events",
                column: "dispatch_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispatches_departure_date",
                table: "dispatches",
                column: "departure_date");

            migrationBuilder.CreateIndex(
                name: "ix_dispatches_internal_reference",
                table: "dispatches",
                column: "internal_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dispatches_related_order_id",
                table: "dispatches",
                column: "related_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispatches_status",
                table: "dispatches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_order_attached_files_order_id",
                table: "order_attached_files",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_timeline_events_order_id",
                table: "order_timeline_events",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_code",
                table: "orders",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_reception_evidences_reception_id",
                table: "reception_evidences",
                column: "reception_id");

            migrationBuilder.CreateIndex(
                name: "ix_reception_items_reception_id",
                table: "reception_items",
                column: "reception_id");

            migrationBuilder.CreateIndex(
                name: "uq_receptions_dispatch_id",
                table: "receptions",
                column: "dispatch_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_route_waypoints_route_id",
                table: "route_waypoints",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "uq_routes_dispatch_id",
                table: "routes",
                column: "dispatch_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_positions_route_id_reported_at",
                table: "telemetry_positions",
                columns: new[] { "route_id", "reported_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transit_alerts_route_id",
                table: "transit_alerts",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_gps_device_id",
                table: "vehicles",
                column: "gps_device_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_license_plate",
                table: "vehicles",
                column: "license_plate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discrepancy_case_events");

            migrationBuilder.DropTable(
                name: "dispatch_items");

            migrationBuilder.DropTable(
                name: "dispatch_timeline_events");

            migrationBuilder.DropTable(
                name: "geofences");

            migrationBuilder.DropTable(
                name: "order_attached_files");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "order_timeline_events");

            migrationBuilder.DropTable(
                name: "reception_evidences");

            migrationBuilder.DropTable(
                name: "reception_items");

            migrationBuilder.DropTable(
                name: "route_waypoints");

            migrationBuilder.DropTable(
                name: "telemetry_positions");

            migrationBuilder.DropTable(
                name: "transit_alerts");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "discrepancy_cases");

            migrationBuilder.DropTable(
                name: "dispatches");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "receptions");

            migrationBuilder.DropTable(
                name: "routes");
        }
    }
}
