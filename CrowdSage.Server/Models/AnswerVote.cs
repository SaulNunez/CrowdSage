using CrowdSage.Server.Models.Enums;

namespace CrowdSage.Server.Models;

public class AnswerVote
{
    public Guid Id { get; set; }
    public Guid AnswerId { get; set; }
    public Answer Answer { get; set; }
    public CrowdsageUser User { get; set; }
    public string UserId { get; set; }
    public VoteValue Vote { get; set; }
}