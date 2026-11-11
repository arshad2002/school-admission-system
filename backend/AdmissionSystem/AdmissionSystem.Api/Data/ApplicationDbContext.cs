using AdmissionSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdmissionSystem.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClassRoom> Classes { get; set; } = null!;
    public DbSet<SeatHold> Holds { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SeatHold>()
            .HasIndex(h => h.Status);

        modelBuilder.Entity<SeatHold>()
            .HasIndex(h => h.ExpiresAt);
    }
}