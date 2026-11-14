using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalReceptionVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "delivery_guide_received",
                table: "receptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_physical_verification",
                table: "receptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "material_good_condition",
                table: "receptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "observations_recorded",
                table: "receptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "quantity_verified",
                table: "receptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "verified_by_user_id",
                table: "receptions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "delivery_guide_received",
                table: "receptions");

            migrationBuilder.DropColumn(
                name: "is_physical_verification",
                table: "receptions");

            migrationBuilder.DropColumn(
                name: "material_good_condition",
                table: "receptions");

            migrationBuilder.DropColumn(
                name: "observations_recorded",
                table: "receptions");

            migrationBuilder.DropColumn(
                name: "quantity_verified",
                table: "receptions");

            migrationBuilder.DropColumn(
                name: "verified_by_user_id",
                table: "receptions");
        }
    }
}
