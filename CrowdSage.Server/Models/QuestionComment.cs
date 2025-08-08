namespace CrowdSage.Server.Models;

public class QuestionComment
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Question Question { get; set; }
}