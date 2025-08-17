using System.ComponentModel.DataAnnotations;

namespace CrowdSage.Server.Models;

public class Question
{
    public Guid Id { get; set; }
    [MaxLength(256)]
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset EditedAt { get; set; }
    public List<string> Tags { get; set; }

    public List<Answer> Answers { get; set; }
    public List<QuestionComment> Comments { get; set; }
}
