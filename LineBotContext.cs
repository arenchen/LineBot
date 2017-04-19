using System.IO;
using LineBot.Models;
using Microsoft.EntityFrameworkCore;

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
