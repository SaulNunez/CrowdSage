using System.ComponentModel.DataAnnotations;

namespace CrowdSage.Server.Models;

public class MediaAsset
{
    public Guid Id { get; set; }
    [MaxLength(256)]
    public string ObjectKey { get; set; }
    [MaxLength(64)]
    public string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public CrowdsageUser Uploader { get; set; }
    public string UploaderId { get; set; }
}
