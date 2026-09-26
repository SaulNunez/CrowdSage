namespace CrowdSage.Server.Models.Outputs;

public record MediaDto
{
    public required Guid Id { get; init; }
    public required string Url { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
}
