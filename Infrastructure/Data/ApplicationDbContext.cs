using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Identity;

namespace WebApp.Infrastructure.Data;

/// <summary>
/// Applikationens EF Core-kontext. Innehåller både Identity-tabeller och domänens tabeller.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Domänmodellen (CV).
    public DbSet<Profile> Profiler => Set<Profile>();
    public DbSet<Skill> Kompetenser => Set<Skill>();
    public DbSet<Project> Projekt => Set<Project>();
    public DbSet<ProjectUser> ProjektAnvandare => Set<ProjectUser>();
    public DbSet<ProfileVisit> ProfilBesok => Set<ProfileVisit>();

    public DbSet<UserMessage> UserMessages => Set<UserMessage>();

    // Äldre entitet som kan finnas kvar för bakåtkompatibilitet.
    public DbSet<Message> Meddelanden => Set<Message>();

    public DbSet<ApplicationUserProfile> ApplicationUserProfiles => Set<ApplicationUserProfile>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Index och constraints sätts med Fluent API för att stödja vanliga sökningar och förhindra dubletter.

        builder.Entity<Profile>()
            .HasIndex(p => p.OwnerUserId);

        builder.Entity<Profile>()
            .HasIndex(p => p.IsPublic);

        builder.Entity<Profile>()
            .HasIndex(p => p.CreatedUtc);

        builder.Entity<Project>()
            .HasIndex(p => p.CreatedUtc);

        builder.Entity<Project>()
            .HasIndex(p => p.CreatedByUserId);

        builder.Entity<ProjectUser>()
            .HasIndex(x => new { x.ProjectId, x.UserId })
            .IsUnique();

        builder.Entity<ApplicationUserProfile>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        builder.Entity<ApplicationUserProfile>()
            .HasOne(x => x.Profile)
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ConversationParticipant>()
            .HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique();

        builder.Entity<DirectMessage>()
            .HasIndex(m => m.ConversationId);

        builder.Entity<DirectMessage>()
            .HasIndex(m => m.SentUtc);

        builder.Entity<ProfileVisit>()
            .HasIndex(v => v.ProfileId);

        builder.Entity<ProfileVisit>()
            .HasIndex(v => v.VisitedUtc);

        builder.Entity<UserMessage>()
            .HasIndex(m => new { m.RecipientUserId, m.IsRead, m.SentUtc });

        builder.Entity<UserMessage>()
            .HasIndex(m => m.SenderUserId);
    }
}