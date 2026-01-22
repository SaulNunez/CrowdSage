namespace CrowdSage.Server.Models.Outputs;

public record QuestionCommentDto
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required AuthorDto Author { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}