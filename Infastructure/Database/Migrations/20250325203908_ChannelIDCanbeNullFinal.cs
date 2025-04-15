using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChannelIDCanbeNullFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Channels_ChannelId",
                table: "Posts");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Channels_ChannelId",
                table: "Posts",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Channels_ChannelId",
                table: "Posts");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Channels_ChannelId",
                table: "Posts",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
