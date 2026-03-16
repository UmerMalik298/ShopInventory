using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopInventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class oldPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OldPrice",
                table: "Product",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldPrice",
                table: "Product");
        }
    }
}
