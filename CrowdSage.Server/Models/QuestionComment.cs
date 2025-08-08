namespace CrowdSage.Server.Models;

public class QuestionComment
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string CommentText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Question Question { get; set; }
}