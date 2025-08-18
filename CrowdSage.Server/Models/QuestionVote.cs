using CrowdSage.Server.Models.Enums;

namespace CrowdSage.Server.Models;

public class QuestionVote
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Question Question { get; set; }
    public string UserId { get; set; }
    public VoteValue VoteValue { get; set; }
}