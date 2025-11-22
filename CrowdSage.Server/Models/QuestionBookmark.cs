using Microsoft.AspNetCore.Identity;

namespace CrowdSage.Server.Models;

public class QuestionBookmark
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Question Question { get; set; }
    public string UserId { get; set; }
    public CrowdsageUser User { get; set; }
}