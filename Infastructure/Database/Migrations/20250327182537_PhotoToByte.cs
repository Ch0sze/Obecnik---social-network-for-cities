using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infastructure.Database.Migrations
{
    public partial class PhotoToByte : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the default constraint if it exists
            migrationBuilder.Sql(@"
                DECLARE @constraintName NVARCHAR(200);
                SELECT @constraintName = d.name
                FROM sys.default_constraints d
                INNER JOIN sys.columns c 
                    ON d.parent_column_id = c.column_id 
                    AND d.parent_object_id = c.object_id
                WHERE d.parent_object_id = OBJECT_ID(N'Posts') 
                    AND c.name = 'Photo';
                
                IF @constraintName IS NOT NULL
                    EXEC('ALTER TABLE Posts DROP CONSTRAINT [' + @constraintName + ']');
            ");

            // Step 2: Add a temporary column of type varbinary
            migrationBuilder.AddColumn<byte[]>(
                name: "TempPhoto",
                table: "Posts",
                type: "varbinary(max)",
                nullable: true);

            // Step 3: Convert the existing 'Photo' data from nvarchar to varbinary using the temporary column
            migrationBuilder.Sql(@"
                UPDATE Posts
                SET TempPhoto = CONVERT(varbinary(max), Photo)
                WHERE Photo IS NOT NULL;
            ");

            // Step 4: Drop the original 'Photo' column
            migrationBuilder.DropColumn(
                name: "Photo",
                table: "Posts");

            // Step 5: Rename the temporary column to 'Photo'
            migrationBuilder.RenameColumn(
                name: "TempPhoto",
                table: "Posts",
                newName: "Photo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert the 'Photo' column from varbinary back to string
            migrationBuilder.AddColumn<string>(
                name: "TempPhoto",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            // Convert varbinary back to string (if necessary)
            migrationBuilder.Sql(@"
                UPDATE Posts
                SET TempPhoto = CONVERT(nvarchar(max), Photo)
                WHERE Photo IS NOT NULL;
            ");

            // Drop the 'Photo' column after conversion
            migrationBuilder.DropColumn(
                name: "Photo",
                table: "Posts");

            // Rename the temporary column back to 'Photo'
            migrationBuilder.RenameColumn(
                name: "TempPhoto",
                table: "Posts",
                newName: "Photo");
        }
    }
}
