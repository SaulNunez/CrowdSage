using CrowdSage.Server.Models;
using CrowdSage.Server.Models.Outputs;
using Microsoft.Extensions.Options;

namespace CrowdSage.Server.Services;

public class MediaService(CrowdsageDbContext dbContext, IMediaStorage storage, IOptions<MediaStorageOptions> options) : IMediaService
{
    public async Task<MediaDto> UploadImageAsync(Stream content, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(userId);

        var maxBytes = options.Value.MaxBytes;

        // Buffer at most one byte past the limit, so an oversized upload is
        // detected without reading all of it, and non-seekable streams work.
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > maxBytes)
            {
                throw new ArgumentException($"Image exceeds the maximum size of {maxBytes} bytes.", nameof(content));
            }
        }

        if (buffer.Length == 0)
        {
            throw new ArgumentException("Image is empty.", nameof(content));
        }

        // The client's declared content type and file name are ignored; only
        // these raster formats are accepted. SVG is deliberately excluded
        // because it can carry script.
        var (contentType, extension) = SniffImageType(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))
            ?? throw new ArgumentException("Unsupported image format. Use PNG, JPEG, GIF or WebP.", nameof(content));

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var objectKey = $"{now:yyyy}/{now:MM}/{id}.{extension}";

        buffer.Position = 0;
        await storage.SaveAsync(objectKey, buffer, cancellationToken);

        var asset = new MediaAsset
        {
            Id = id,
            ObjectKey = objectKey,
            ContentType = contentType,
            SizeBytes = buffer.Length,
            CreatedAt = now,
            UploaderId = userId
        };
        dbContext.MediaAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MediaDto
        {
            Id = asset.Id,
            Url = $"/api/media/{asset.Id}",
            ContentType = asset.ContentType,
            SizeBytes = asset.SizeBytes
        };
    }

    public async Task<(Stream Content, string ContentType)> OpenAsync(Guid id)
    {
        var asset = await dbContext.MediaAssets.FindAsync(id)
            ?? throw new KeyNotFoundException($"Media with ID {id} not found.");
        return (storage.OpenRead(asset.ObjectKey), asset.ContentType);
    }

    private static (string ContentType, string Extension)? SniffImageType(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
            return ("image/png", "png");
        if (data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
            return ("image/jpeg", "jpg");
        if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8))
            return ("image/gif", "gif");
        if (data.Length >= 12 && data.StartsWith("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8))
            return ("image/webp", "webp");
        return null;
    }
}

public interface IMediaService
{
    Task<MediaDto> UploadImageAsync(Stream content, string userId, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType)> OpenAsync(Guid id);
}
