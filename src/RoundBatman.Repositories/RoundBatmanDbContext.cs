using Microsoft.EntityFrameworkCore;
using RoundBatman.Domain;

namespace RoundBatman.Repositories;

public class RoundBatmanDbContext(DbContextOptions<RoundBatmanDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(team => team.Id);
            entity.Property(team => team.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(team => team.Name).IsUnique();
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.HasKey(tournament => tournament.Id);
            entity.Property(tournament => tournament.Name).HasMaxLength(200).IsRequired();
            entity.Ignore(tournament => tournament.Status);

            entity.OwnsOne(tournament => tournament.Format, format =>
            {
                format.Property(value => value.Type).HasConversion<string>().HasMaxLength(50);
            });

            entity.HasMany(tournament => tournament.Groups)
                .WithOne()
                .HasForeignKey(group => group.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(tournament => tournament.Matches)
                .WithOne()
                .HasForeignKey(match => match.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(group => group.Id);
            entity.Property(group => group.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(group => new { group.TournamentId, group.Name }).IsUnique();

            entity.HasMany(group => group.Teams)
                .WithMany()
                .UsingEntity(join => join.ToTable("GroupTeams"));
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(match => match.Id);
            entity.Ignore(match => match.Winner);
            entity.Ignore(match => match.IsCompleted);
            entity.HasIndex(match => new { match.TournamentId, match.GroupId });

            entity.OwnsOne(match => match.Score, score =>
            {
                score.Property<bool>("HasExplicitResult");
            });

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(match => match.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(match => match.VisitorTeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
