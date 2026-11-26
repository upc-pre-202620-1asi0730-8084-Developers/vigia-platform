using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddContactRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    request_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    full_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    company_name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    email = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    segment = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    plan = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    message = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: false),
                    locale = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact_requests", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_contact_requests_created_at_utc",
                table: "contact_requests",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_contact_requests_email",
                table: "contact_requests",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_contact_requests_request_id",
                table: "contact_requests",
                column: "request_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_requests");
        }
    }
}
