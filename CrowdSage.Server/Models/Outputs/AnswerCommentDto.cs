namespace CrowdSage.Server.Models.Outputs;

public record AnswerCommentDto
{
    public Guid Id { get; init; }
    public string Content { get; init; }
    public AuthorDto Author { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}