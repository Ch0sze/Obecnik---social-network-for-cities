using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPinnedToPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF NOT EXISTS (SELECT 1 FROM sys.columns 
                      WHERE object_id = OBJECT_ID('Posts') 
                      AND name = 'IsPinned')
        BEGIN
            ALTER TABLE [Posts] ADD [IsPinned] bit NOT NULL DEFAULT 0;
        END
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF EXISTS (SELECT 1 FROM sys.columns 
                  WHERE object_id = OBJECT_ID('Posts') 
                  AND name = 'IsPinned')
        BEGIN
            ALTER TABLE [Posts] DROP COLUMN [IsPinned];
        END
    ");
        }
    }
}
