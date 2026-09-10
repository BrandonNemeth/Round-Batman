namespace RoundBatman.Domain;

public class Score
{
    public int HomeTeamScore { get; set; }
    public int VisitorTeamScore { get; set; }

    public bool IsCompleted => HomeTeamScore >= 0 && VisitorTeamScore >= 0 &&
                                (HomeTeamScore > 0 || VisitorTeamScore > 0 || HasExplicitResult);

    private bool HasExplicitResult { get; set; }

    public void SetResult(int homeScore, int visitorScore)
    {
        if (homeScore < 0 || visitorScore < 0)
            throw new ArgumentException("Scores cannot be negative.");

        HomeTeamScore = homeScore;
        VisitorTeamScore = visitorScore;
        HasExplicitResult = true;
    }

    public Enums.Winner? GetWinner()
    {
        if (!HasExplicitResult) return null;
        if (HomeTeamScore == VisitorTeamScore) return null;
        return HomeTeamScore > VisitorTeamScore ? Enums.Winner.HOME : Enums.Winner.VISITOR;
    }
}
