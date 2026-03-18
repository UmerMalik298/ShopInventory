using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopInventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftPropertyInBills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Bill",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "Bill");
        }
    }
}
