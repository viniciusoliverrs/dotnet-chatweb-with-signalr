using Microsoft.EntityFrameworkCore;

namespace ChatWeb.Models.Context
{
    public class ChatContext : DbContext
    {
        public ChatContext(DbContextOptions<ChatContext> option) : base(option) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Grupos { get; set; }
        public DbSet<Message> Messages { get; set; }

    }
}
