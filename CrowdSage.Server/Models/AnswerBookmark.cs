using Microsoft.AspNetCore.Identity;

namespace CrowdSage.Server.Models;

public class AnswerBookmark
{
    public Guid Id { get; set; }
    public Guid AnswerId { get; set; }
    public Answer Answer { get; set; }
    public string UserId { get; set; }
    public CrowdsageUser User { get; set; }
}