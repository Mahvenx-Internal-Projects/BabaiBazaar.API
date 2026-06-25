using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BabaiBazaar.API.Migrations
{
    /// <inheritdoc />
    public partial class AadhaarRequiredForDeliveryBoy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DeliveryBoys",
                keyColumn: "AadhaarNumber",
                keyValue: null,
                column: "AadhaarNumber",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "AadhaarNumber",
                table: "DeliveryBoys",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AadhaarNumber",
                table: "DeliveryBoys",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
