namespace CrowdSage.Server.Models.Outputs;

public record AnswerDto
{
    public Guid Id { get; init; }
    public string Content { get; init; }
    public int Votes { get; init; }
    public bool Bookmarked { get; init; }
    public AuthorDto Author { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}