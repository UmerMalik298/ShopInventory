using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopInventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DraftBills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Bill",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Bill");
        }
    }
}
