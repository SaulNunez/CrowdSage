namespace CrowdSage.Server.Models.Outputs;

public record AuthorDto
{
    public string Id { get; init; }
    public string? UrlPhoto { get; init; }
    public string UserName { get; init; }
}