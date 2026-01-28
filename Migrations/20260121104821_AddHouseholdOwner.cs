using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseholdExpenses.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OwedAmount",
                table: "ExpenseShares",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Expenses",
                newName: "Description");

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Households",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ExpenseShares",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ExpenseShares");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "ExpenseShares",
                newName: "OwedAmount");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Expenses",
                newName: "Title");
        }
    }
}
