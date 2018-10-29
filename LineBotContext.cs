using System.IO;
using Microsoft.EntityFrameworkCore;
using LineBot.Models;

namespace LineBot
{
    public class LineBotContext : DbContext
    {
        public LineBotContext(DbContextOptions<LineBotContext> options) : base(options)
        {
            if (!File.Exists("LineBot.sqlite"))
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
            }
        }

        public DbSet<KeyDictionary> KeyDictionaries { get; set; }
    }
}
