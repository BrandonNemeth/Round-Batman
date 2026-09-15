namespace RoundBatman.Repositories;

/// <summary>
/// Contrato genérico para las operaciones comunes de persistencia.
/// TKey permite que cada entidad use la clave que realmente necesita.
/// </summary>
public interface IRepository<TEntity, in TKey>
{
    Task<TEntity?> GetByIdAsync(TKey key);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task<bool> DeleteAsync(TKey key);
}

/// <summary>
/// Un grupo se identifica dentro del contexto de un torneo.
/// </summary>
public readonly record struct GroupRepositoryKey(string TournamentId, string GroupId);
