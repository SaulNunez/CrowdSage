namespace CrowdSage.Server.Models.Outputs;

public record AuthorDto
{
    public required string Id { get; init; }
    public required string? UrlPhoto { get; init; }
    public required string UserName { get; init; }
}