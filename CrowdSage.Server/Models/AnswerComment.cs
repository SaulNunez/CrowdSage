namespace CrowdSage.Server.Models;

public class AnswerComment
{
    public Guid Id { get; set; }
    public Guid AnswerId { get; set; }
    public Answer Answer { get; set; }
    public string Content { get; set; }
    public CrowdsageUser Author { get; set; }
    public string AuthorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}