using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredEvidencePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stored_evidence_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    reception_id = table.Column<int>(type: "int", nullable: false),
                    evidence_id = table.Column<int>(type: "int", nullable: false),
                    request_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    fingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    content = table.Column<byte[]>(type: "mediumblob", nullable: false),
                    content_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    file_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    sha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    uploaded_by_account_id = table.Column<int>(type: "int", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stored_evidence_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_stored_evidence_files_reception_evidences_evidence_id",
                        column: x => x.evidence_id,
                        principalTable: "reception_evidences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stored_evidence_files_receptions_reception_id",
                        column: x => x.reception_id,
                        principalTable: "receptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_stored_evidence_files_evidence_id",
                table: "stored_evidence_files",
                column: "evidence_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stored_evidence_files_reception_id_request_id",
                table: "stored_evidence_files",
                columns: new[] { "reception_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stored_evidence_files");
        }
    }
}
