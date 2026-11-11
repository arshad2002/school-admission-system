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

        // Use PostgreSQL xmin system column as an optimistic concurrency token
        modelBuilder.Entity<ClassRoom>()
            .Property(c => c.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        modelBuilder.Entity<SeatHold>()
            .HasIndex(h => h.Status);

        modelBuilder.Entity<SeatHold>()
            .HasIndex(h => h.ExpiresAt);

        // Composite index for fast lookups: active holds per student+class
        modelBuilder.Entity<SeatHold>()
            .HasIndex(h => new { h.ClassId, h.StudentId, h.Status });
    }
}