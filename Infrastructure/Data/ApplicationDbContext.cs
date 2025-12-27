using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Identity;

namespace WebApp.Infrastructure.Data;

/// <summary>
/// Applikationens EF Core-kontext som innehåller Identity-tabeller och domänens tabeller.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Domänmodellens DbSet: profiler, CV-relaterade entiteter och meddelandesystem
    public DbSet<Profile> Profiler => Set<Profile>();
    public DbSet<Education> Utbildningar => Set<Education>();
    public DbSet<Skill> Kompetenser => Set<Skill>();
    public DbSet<WorkExperience> Erfarenheter => Set<WorkExperience>();
    public DbSet<Project> Projekt => Set<Project>();
    public DbSet<ProjectUser> ProjektAnvandare => Set<ProjectUser>();
    public DbSet<ProfileVisit> ProfilBesok => Set<ProfileVisit>();

    public DbSet<UserMessage> UserMessages => Set<UserMessage>();

    // Äldre entitet för bakåtkompatibilitet
    public DbSet<Message> Meddelanden => Set<Message>();

    public DbSet<ApplicationUserProfile> ApplicationUserProfiles => Set<ApplicationUserProfile>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Fluent API: index och constraints för vanliga sökningar och förhindrande av dubletter

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

        // Unikt sammansatt index för att förhindra duplicerade projekt/användare-relationer
        builder.Entity<ProjectUser>()
            .HasIndex(x => new { x.ProjectId, x.UserId })
            .IsUnique();

        // En användare får högst en koppling till en profil (1:1)
        builder.Entity<ApplicationUserProfile>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        builder.Entity<ApplicationUserProfile>()
            .HasOne(x => x.Profile)
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            // Cascade delete: ta bort kopplingen om profilen tas bort
            .OnDelete(DeleteBehavior.Cascade);

        // Unikt sammansatt index för att förhindra duplicerade deltagare i en konversation
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

        // Index för snabba frågor över mottagare, läst-status och tid
        builder.Entity<UserMessage>()
            .HasIndex(m => new { m.RecipientUserId, m.IsRead, m.SentUtc });

        builder.Entity<UserMessage>()
            .HasIndex(m => m.SenderUserId);

        builder.Entity<Education>()
            .HasIndex(x => new { x.ProfileId, x.SortOrder });

        builder.Entity<Education>()
            .HasOne(x => x.Profile)
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            // Cascade delete: ta bort utbildningsposter när profilen tas bort
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkExperience>()
            .HasIndex(x => new { x.ProfileId, x.SortOrder });

        builder.Entity<WorkExperience>()
            .HasOne(x => x.Profile)
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            // Cascade delete: ta bort erfarenheter när profilen tas bort
            .OnDelete(DeleteBehavior.Cascade);
    }
}