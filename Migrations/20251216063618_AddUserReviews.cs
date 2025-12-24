using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo_Food_Main.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "Feedback");

            migrationBuilder.AddColumn<int>(
                name: "MenuItemId",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_MenuItemId",
                table: "Feedback",
                column: "MenuItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedback_MenuItems_MenuItemId",
                table: "Feedback",
                column: "MenuItemId",
                principalTable: "MenuItems",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedback_MenuItems_MenuItemId",
                table: "Feedback");

            migrationBuilder.DropIndex(
                name: "IX_Feedback_MenuItemId",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "MenuItemId",
                table: "Feedback");

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "Feedback",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
