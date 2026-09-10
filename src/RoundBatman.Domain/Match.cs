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
    public bool IsCompleted => Score.GetWinner() is not null || Score.HomeTeamScore != Score.VisitorTeamScore;
}
