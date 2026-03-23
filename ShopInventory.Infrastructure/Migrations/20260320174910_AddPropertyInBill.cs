using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopInventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyInBill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InventoryDeducted",
                table: "Bill",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventoryDeducted",
                table: "Bill");
        }
    }
}
