namespace RoundBatman.Domain.Scheduling;

public readonly record struct MatchPairing(string HomeTeamId, string VisitorTeamId);

public static class RoundRobinScheduler
{
    /// <summary>
    /// Genera el calendario de partidos round robin (una sola vuelta) para
    /// un conjunto de equipos, usando el método del círculo.
    /// </summary>
    public static List<MatchPairing> GenerateSchedule(IReadOnlyList<Team> teams)
    {
        ArgumentNullException.ThrowIfNull(teams);

        if (teams.Count < 2)
            return [];

        var ids = teams.Select(t => t.Id).ToList();

        var hasBye = ids.Count % 2 != 0;
        if (hasBye)
            ids.Add(null!); // hueco: el equipo emparejado con esto descansa esa ronda

        var n = ids.Count;
        var rounds = n - 1;
        var half = n / 2;

        var pairings = new List<MatchPairing>();

        // ids[0] queda fijo; el resto rota cada ronda
        var rotating = ids.Skip(1).ToList();

        for (var round = 0; round < rounds; round++)
        {
            var roundIds = new List<string?> { ids[0] };
            roundIds.AddRange(rotating);

            for (var i = 0; i < half; i++)
            {
                var a = roundIds[i];
                var b = roundIds[n - 1 - i];

                if (a is null || b is null)
                    continue; // uno de los dos tiene bye esta ronda

                // alterna local/visitante entre rondas para repartir localías
                pairings.Add(round % 2 == 0
                    ? new MatchPairing(a, b)
                    : new MatchPairing(b, a));
            }

            // rotación: el último elemento pasa al frente
            var last = rotating[^1];
            rotating.RemoveAt(rotating.Count - 1);
            rotating.Insert(0, last);
        }

        return pairings;
    }
}
