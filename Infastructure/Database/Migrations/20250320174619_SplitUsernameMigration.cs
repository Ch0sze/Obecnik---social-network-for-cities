using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SplitUsernameMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Firstname",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET Firstname = CASE 
                    WHEN CHARINDEX(' ', Name) > 0 THEN LEFT(Name, CHARINDEX(' ', Name) - 1) 
                    ELSE Name 
                END;
            ");
            
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET LastName = CASE 
                    WHEN CHARINDEX(' ', Name) > 0 THEN RIGHT(Name, LEN(Name) - CHARINDEX(' ', Name)) 
                    ELSE '' 
                END;
            ");
            
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
