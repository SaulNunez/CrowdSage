using CrowdSage.Server.Models.Enums;

namespace CrowdSage.Server.Models.Outputs;

public record AnswerDto
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required int Votes { get; init; }
    public bool Bookmarked { get; init; }
    public AuthorDto Author { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public required VoteValue? CurrentUserVote { get; init; }
}