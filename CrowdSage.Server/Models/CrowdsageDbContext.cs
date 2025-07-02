using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Models;

public class CrowdsageDbContext(DbContextOptions<CrowdsageDbContext> options) : DbContext(options)
{
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }
}
