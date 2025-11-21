using CrowdSage.Server.Models.Enums;

namespace CrowdSage.Server.Models.InsertUpdate;

public record VoteInput
{
    public VoteValue Vote { get; init; }
}