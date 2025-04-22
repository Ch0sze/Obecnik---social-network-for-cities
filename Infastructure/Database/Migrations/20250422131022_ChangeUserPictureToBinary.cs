using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserPictureToBinary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First add a new column to hold the binary data
            migrationBuilder.AddColumn<byte[]>(
                name: "PictureTemp",
                table: "Users",
                nullable: true);
            
            // If you have existing string data that needs conversion (e.g., base64 strings)
            // You would need to write SQL to convert it here
            // This is just an example - adjust based on your actual data format
            migrationBuilder.Sql(@"
            UPDATE Users 
            SET PictureTemp = CONVERT(varbinary(max), Picture)
            WHERE Picture IS NOT NULL
        ");
        
            // Remove the old column
            migrationBuilder.DropColumn(
                name: "Picture",
                table: "Users");
            
            // Rename the new column
            migrationBuilder.RenameColumn(
                name: "PictureTemp",
                table: "Users",
                newName: "Picture");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the process for rollback
            migrationBuilder.AddColumn<string>(
                name: "PictureTemp",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
            
            migrationBuilder.Sql(@"
            UPDATE Users 
            SET PictureTemp = CONVERT(nvarchar(max), Picture)
            WHERE Picture IS NOT NULL
        ");
        
            migrationBuilder.DropColumn(
                name: "Picture",
                table: "Users");
            
            migrationBuilder.RenameColumn(
                name: "PictureTemp",
                table: "Users",
                newName: "Picture");
        }
    }
}
