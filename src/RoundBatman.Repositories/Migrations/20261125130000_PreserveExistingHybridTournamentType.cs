using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoundBatman.Repositories.Migrations;

public partial class PreserveExistingHybridTournamentType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Before explicit hybrid mode existed, ROUND_ROBIN automatically advanced
        // to knockout. Preserve that behavior for tournaments already in the DB.
        migrationBuilder.Sql(
            "UPDATE \"Tournaments\" SET \"Format_Type\" = 'HYBRID' " +
            "WHERE \"Format_Type\" IN ('ROUND_ROBIN', 'NFL');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE \"Tournaments\" SET \"Format_Type\" = 'ROUND_ROBIN' " +
            "WHERE \"Format_Type\" = 'HYBRID';");
    }
}
