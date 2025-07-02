namespace CrowdSage.Server.Models;

public class Answer
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset EditedAt { get; set; }
    public Question Question { get; set; }
}
