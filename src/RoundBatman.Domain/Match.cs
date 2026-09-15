namespace RoundBatman.Domain;

public class Match
{
    public string Id { get; set; } = string.Empty;
    public string TournamentId { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    public string HomeTeamId { get; set; } = string.Empty;
    public string VisitorTeamId { get; set; } = string.Empty;
    public Score Score { get; set; } = new();

    public Enums.Winner? Winner => Score.GetWinner();
    // Score.IsCompleted distingue un empate registrado (incluso 0-0)
    // de un partido que todavía no tiene resultado.
    public bool IsCompleted => Score.IsCompleted;
}
