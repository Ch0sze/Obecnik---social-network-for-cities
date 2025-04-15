using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infastructure.Database.Migrations
{
    public partial class CommunityPicturePrepare : Migration
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
                WHERE d.parent_object_id = OBJECT_ID(N'Communities') 
                    AND c.name = 'Picture';
                
                IF @constraintName IS NOT NULL
                    EXEC('ALTER TABLE Communities DROP CONSTRAINT [' + @constraintName + ']');
            ");

            // Step 2: Add a temporary column of type varbinary
            migrationBuilder.AddColumn<byte[]>(
                name: "TempPicture",
                table: "Communities",
                type: "varbinary(max)",
                nullable: true);

            // Step 3: Convert the existing 'Picture' data from nvarchar to varbinary using the temporary column
            migrationBuilder.Sql(@"
                UPDATE Communities
                SET TempPicture = CONVERT(varbinary(max), Picture)
                WHERE Picture IS NOT NULL;
            ");

            // Step 4: Drop the original 'Picture' column
            migrationBuilder.DropColumn(
                name: "Picture",
                table: "Communities");

            // Step 5: Rename the temporary column to 'Picture'
            migrationBuilder.RenameColumn(
                name: "TempPicture",
                table: "Communities",
                newName: "Picture");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary column of type nvarchar
            migrationBuilder.AddColumn<string>(
                name: "TempPicture",
                table: "Communities",
                type: "nvarchar(max)",
                nullable: true);

            // Step 2: Convert varbinary back to string (if necessary)
            migrationBuilder.Sql(@"
                UPDATE Communities
                SET TempPicture = CONVERT(nvarchar(max), Picture)
                WHERE Picture IS NOT NULL;
            ");

            // Step 3: Drop the 'Picture' column after conversion
            migrationBuilder.DropColumn(
                name: "Picture",
                table: "Communities");

            // Step 4: Rename the temporary column back to 'Picture'
            migrationBuilder.RenameColumn(
                name: "TempPicture",
                table: "Communities",
                newName: "Picture");
        }
    }
}
