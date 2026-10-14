using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Email must be unique (same as UNIQUE in the Part 1 SQL script)
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // Users (Organiser) 1 --- * Events
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Events 1 --- * Categories (deleting an event deletes its categories)
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Users (Participant) 1 --- * Enrolments
        // Restrict here, otherwise SQL Server complains about "multiple cascade paths"
        modelBuilder.Entity<Enrolment>()
            .HasOne(en => en.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(en => en.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categories 1 --- * Enrolments
        modelBuilder.Entity<Enrolment>()
            .HasOne(en => en.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(en => en.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrolments 1 --- 1 Results (EnrolmentId is UNIQUE in Results)
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithOne(en => en.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrolments 1 --- 1 Payments (EnrolmentId is UNIQUE in Payments)
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Enrolment)
            .WithOne(en => en.Payment)
            .HasForeignKey<Payment>(p => p.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
