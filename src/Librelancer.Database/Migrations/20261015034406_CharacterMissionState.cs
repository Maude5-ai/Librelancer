using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibreLancer.Database.Migrations
{
    /// <inheritdoc />
    public partial class CharacterMissionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MissionState",
                table: "Characters",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MissionState",
                table: "Characters");
        }
    }
}
