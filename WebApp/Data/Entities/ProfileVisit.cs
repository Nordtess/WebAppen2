using System;

namespace WebApp.Data.Entities
{
    public class ProfileVisit
    {
        public int Id { get; set; }
        public int ProfilId { get; set; }
        public int Besökandeprofil { get; set; }
        public DateTime Besöksdatum { get; set; }
    }
}
