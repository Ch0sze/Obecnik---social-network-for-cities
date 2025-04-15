using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminRequestTableAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AdminRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "AdminRequests");
        }
    }
}
