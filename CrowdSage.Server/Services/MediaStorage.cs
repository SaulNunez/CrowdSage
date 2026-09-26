using Microsoft.Extensions.Options;

namespace CrowdSage.Server.Services;

public class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    // Relative paths are resolved against the content root.
    public string RootPath { get; set; } = "App_Data/media";
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}

public class LocalDiskMediaStorage : IMediaStorage
{
    private readonly string rootPath;

    public LocalDiskMediaStorage(IOptions<MediaStorageOptions> options, IWebHostEnvironment environment)
    {
        rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
        Directory.CreateDirectory(rootPath);
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Stream OpenRead(string key)
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            throw new KeyNotFoundException($"Media object {key} not found.");
        }
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
    }

    private string ResolvePath(string key)
    {
        var path = Path.GetFullPath(Path.Combine(rootPath, key));
        if (!path.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Media key {key} resolves outside the storage root.", nameof(key));
        }
        return path;
    }
}

public interface IMediaStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default);
    Stream OpenRead(string key);
}
