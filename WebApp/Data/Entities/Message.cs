using System;

namespace WebApp.Data.Entities
{
    public class Message
    {
        public int Id { get; set; }
        public string? Innehåll { get; set; }
        public DateTime Skickat { get; set; }
    }
}