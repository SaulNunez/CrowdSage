using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Models;

public class CrowdsageDbContext(DbContextOptions<CrowdsageDbContext> options) : IdentityDbContext<CrowdsageUser>(options)
{
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }

    public DbSet<AnswerComment> AnswerComments { get; set; }
    public DbSet<QuestionComment> QuestionComments { get; set; }
    public DbSet<QuestionVote> QuestionVotes { get; set; }
    public DbSet<AnswerVote> AnswerVotes { get; set; }
    public DbSet<QuestionBookmark> QuestionBookmarks { get; set; }
    public DbSet<AnswerBookmark> AnswerBookmarks { get; set; }
}