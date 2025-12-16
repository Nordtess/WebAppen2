using Microsoft.EntityFrameworkCore;
using WebApp.Data.Entities;


namespace WebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Profile> Profiler { get; set; }
        public DbSet<Skill> Kompetenser { get; set; }
        public DbSet<Project> Projekt { get; set; }
        public DbSet<ProjectUser> ProjektAnvändare { get; set; }
        public DbSet<Message> Meddelanden { get; set; }
        public DbSet<ProfileVisit> ProfilBesök { get; set; }

    }

}
