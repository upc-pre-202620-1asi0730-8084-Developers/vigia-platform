using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    action = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    detail = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    user_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    reference = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_records", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_activity_records_occurred_at",
                table: "activity_records",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_records");
        }
    }
}
