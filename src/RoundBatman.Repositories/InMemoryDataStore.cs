using System.Collections.Concurrent;
using RoundBatman.Domain;

namespace RoundBatman.Repositories;


// Reemplazar por EF Core más adelante
// sin tocar las interfaces de los repositorios.

public class InMemoryDataStore
{
    public ConcurrentDictionary<string, Team> Teams { get; } = new();
    public ConcurrentDictionary<string, Tournament> Tournaments { get; } = new();
}
