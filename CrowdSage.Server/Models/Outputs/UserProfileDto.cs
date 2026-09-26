namespace CrowdSage.Server.Models.Outputs;

public record UserProfileDto
{
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public required string? UrlPhoto { get; init; }
    public required int QuestionCount { get; init; }
    public required int AnswerCount { get; init; }
    public required int CommentCount { get; init; }
}

public record UserQuestionSummaryDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required int Votes { get; init; }
    public required int AnswerCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public record UserAnswerSummaryDto
{
    public required Guid Id { get; init; }
    public required Guid QuestionId { get; init; }
    public required string QuestionTitle { get; init; }
    public required string Content { get; init; }
    public required int Votes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public record UserCommentSummaryDto
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required Guid QuestionId { get; init; }
    public required string QuestionTitle { get; init; }
    // Null when the comment was left on the question itself.
    public required Guid? AnswerId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
