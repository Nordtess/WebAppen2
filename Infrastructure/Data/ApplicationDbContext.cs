using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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

    public DbSet<Competence> Kompetenskatalog { get; set; } = null!;
    public DbSet<UserCompetence> AnvandarKompetenser { get; set; } = null!;

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

        // Unik kontroll: en användare kan inte välja samma kompetens två gånger.
        builder.Entity<UserCompetence>()
            .HasIndex(x => new { x.UserId, x.CompetenceId })
            .IsUnique();

        builder.Entity<Competence>()
            .Property(c => c.IsTopList)
            .HasDefaultValue(false);

        builder.Entity<Competence>()
            .Property(c => c.NormalizedName)
            .HasMaxLength(200)
            .HasComputedColumnSql("UPPER(LTRIM(RTRIM([Name])))", stored: true)
            .ValueGeneratedOnAddOrUpdate();

        builder.Entity<Competence>()
            .HasIndex(c => c.NormalizedName)
            .IsUnique();

        // Seed av katalog: en rad per kompetens, "Topplista" styrs av IsTopList
        builder.Entity<Competence>().HasData(CompetenceSeed.All);
    }
}

// Enkel seed-hjälpare
internal static class CompetenceSeed
{
    public static Competence[] All { get; } =
    {
        // Topplista (IsTopList = true) + kategori för ordinarie grupp
        new() { Id = 1, Name = "C#", Category = "Programmeringsspråk", SortOrder = 0, IsTopList = true },
        new() { Id = 2, Name = ".NET", Category = "Backend", SortOrder = 0, IsTopList = true },
        new() { Id = 3, Name = "ASP.NET Core", Category = "Backend", SortOrder = 1, IsTopList = true },
        new() { Id = 4, Name = "MVC", Category = "Backend", SortOrder = 2, IsTopList = true },
        new() { Id = 5, Name = "EF Core", Category = "Backend", SortOrder = 4, IsTopList = true },
        new() { Id = 6, Name = "LINQ", Category = "Backend", SortOrder = 5, IsTopList = true },
        new() { Id = 7, Name = "SQL", Category = "Programmeringsspråk", SortOrder = 5, IsTopList = true },
        new() { Id = 8, Name = "Git", Category = "DevOps & Drift", SortOrder = 0, IsTopList = true },
        new() { Id = 9, Name = "Docker", Category = "DevOps & Drift", SortOrder = 3, IsTopList = true },
        new() { Id = 10, Name = "Azure", Category = "DevOps & Drift", SortOrder = 7, IsTopList = true },
        new() { Id = 11, Name = "Linux", Category = "DevOps & Drift", SortOrder = 5, IsTopList = true },
        new() { Id = 12, Name = "REST API", Category = "Backend", SortOrder = 6, IsTopList = true },

        // Programmeringsspråk
        new() { Id = 14, Name = "Java", Category = "Programmeringsspråk", SortOrder = 1 },
        new() { Id = 15, Name = "Python", Category = "Programmeringsspråk", SortOrder = 2 },
        new() { Id = 16, Name = "JavaScript", Category = "Programmeringsspråk", SortOrder = 3 },
        new() { Id = 17, Name = "TypeScript", Category = "Programmeringsspråk", SortOrder = 4 },
        new() { Id = 19, Name = "HTML", Category = "Programmeringsspråk", SortOrder = 6 },
        new() { Id = 20, Name = "CSS", Category = "Programmeringsspråk", SortOrder = 7 },
        new() { Id = 21, Name = "Bash", Category = "Programmeringsspråk", SortOrder = 8 },

        // Backend
        new() { Id = 25, Name = "Web API", Category = "Backend", SortOrder = 3 },
        new() { Id = 28, Name = "SignalR", Category = "Backend", SortOrder = 7 },

        // Frontend
        new() { Id = 29, Name = "React", Category = "Frontend", SortOrder = 0 },
        new() { Id = 30, Name = "Vue", Category = "Frontend", SortOrder = 1 },
        new() { Id = 31, Name = "Angular", Category = "Frontend", SortOrder = 2 },
        new() { Id = 32, Name = "Vite", Category = "Frontend", SortOrder = 3 },
        new() { Id = 33, Name = "Tailwind", Category = "Frontend", SortOrder = 4 },
        new() { Id = 34, Name = "Bootstrap", Category = "Frontend", SortOrder = 5 },

        // Databaser
        new() { Id = 35, Name = "SQL Server", Category = "Databaser", SortOrder = 0 },
        new() { Id = 36, Name = "PostgreSQL", Category = "Databaser", SortOrder = 1 },
        new() { Id = 37, Name = "MySQL", Category = "Databaser", SortOrder = 2 },
        new() { Id = 38, Name = "SQLite", Category = "Databaser", SortOrder = 3 },
        new() { Id = 39, Name = "MongoDB", Category = "Databaser", SortOrder = 4 },
        new() { Id = 40, Name = "Redis", Category = "Databaser", SortOrder = 5 },

        // DevOps & Drift
        new() { Id = 42, Name = "GitHub", Category = "DevOps & Drift", SortOrder = 1 },
        new() { Id = 43, Name = "CI/CD", Category = "DevOps & Drift", SortOrder = 2 },
        new() { Id = 45, Name = "Kubernetes", Category = "DevOps & Drift", SortOrder = 4 },
        new() { Id = 47, Name = "Nginx", Category = "DevOps & Drift", SortOrder = 6 },
        new() { Id = 49, Name = "AWS", Category = "DevOps & Drift", SortOrder = 8 },

        // Test & Kvalitet
        new() { Id = 50, Name = "xUnit", Category = "Test & Kvalitet", SortOrder = 0 },
        new() { Id = 51, Name = "NUnit", Category = "Test & Kvalitet", SortOrder = 1 },
        new() { Id = 52, Name = "Integration Tests", Category = "Test & Kvalitet", SortOrder = 2 },
        new() { Id = 53, Name = "Unit Tests", Category = "Test & Kvalitet", SortOrder = 3 },
        new() { Id = 54, Name = "Logging", Category = "Test & Kvalitet", SortOrder = 4 },
        new() { Id = 55, Name = "Serilog", Category = "Test & Kvalitet", SortOrder = 5 },

        // Säkerhet
        new() { Id = 56, Name = "OWASP", Category = "Säkerhet", SortOrder = 0 },
        new() { Id = 57, Name = "HTTPS/TLS", Category = "Säkerhet", SortOrder = 1 },
        new() { Id = 58, Name = "JWT", Category = "Säkerhet", SortOrder = 2 },
        new() { Id = 59, Name = "OAuth2", Category = "Säkerhet", SortOrder = 3 },

        // Arkitektur & Metoder
        new() { Id = 60, Name = "Clean Architecture", Category = "Arkitektur & Metoder", SortOrder = 0 },
        new() { Id = 61, Name = "SOLID", Category = "Arkitektur & Metoder", SortOrder = 1 },
        new() { Id = 62, Name = "DDD", Category = "Arkitektur & Metoder", SortOrder = 2 },
        new() { Id = 63, Name = "Agile", Category = "Arkitektur & Metoder", SortOrder = 3 },
        new() { Id = 64, Name = "Scrum", Category = "Arkitektur & Metoder", SortOrder = 4 },
        new() { Id = 65, Name = "TDD", Category = "Arkitektur & Metoder", SortOrder = 5 }
    };
}