using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoundBatman.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchProgressionAndChampion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The model snapshot already contained these columns, so EF produced
            // an empty migration. Add the operations explicitly for databases
            // created from the older InitialCreate migration.
            migrationBuilder.AddColumn<string>(
                name: "ChampionTeamId",
                table: "Tournaments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBye",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Round",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "Matches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TournamentId_Stage_Round",
                table: "Matches",
                columns: new[] { "TournamentId", "Stage", "Round" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_TournamentId_Stage_Round",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Round",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsBye",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ChampionTeamId",
                table: "Tournaments");
        }
    }
}
