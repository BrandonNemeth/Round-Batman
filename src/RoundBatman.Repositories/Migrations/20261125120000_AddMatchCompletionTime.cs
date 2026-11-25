using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoundBatman.Repositories.Migrations;

public partial class AddMatchCompletionTime : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CompletedAt",
            table: "Matches",
            type: "timestamp with time zone",
            nullable: true);

        // Existing results predate completion timestamps. Backfill them so they
        // remain available through the recent-completed-matches endpoint.
        migrationBuilder.Sql(
            "UPDATE \"Matches\" SET \"CompletedAt\" = CURRENT_TIMESTAMP " +
            "WHERE \"Score_HasExplicitResult\" = TRUE;");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_TournamentId_CompletedAt",
            table: "Matches",
            columns: new[] { "TournamentId", "CompletedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Matches_TournamentId_CompletedAt",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "CompletedAt",
            table: "Matches");
    }
}
