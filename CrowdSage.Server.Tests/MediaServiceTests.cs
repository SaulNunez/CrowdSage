using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using CrowdSage.Server.Models;
using CrowdSage.Server.Services;

namespace CrowdSage.Server.Tests;

public class MediaServiceTests
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private sealed class InMemoryMediaStorage : IMediaStorage
    {
        public Dictionary<string, byte[]> Objects { get; } = new();

        public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
        {
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            Objects[key] = copy.ToArray();
        }

        public Stream OpenRead(string key) =>
            Objects.TryGetValue(key, out var bytes)
                ? new MemoryStream(bytes)
                : throw new KeyNotFoundException(key);
    }

    private static CrowdsageDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CrowdsageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CrowdsageDbContext(options);
    }

    private static MediaService CreateService(CrowdsageDbContext context, InMemoryMediaStorage storage, long maxBytes = 1024) =>
        new(context, storage, Options.Create(new MediaStorageOptions { MaxBytes = maxBytes }));

    [Fact]
    public async Task UploadImageAsync_NullStream_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = CreateService(context, new InMemoryMediaStorage());

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.UploadImageAsync(null!, "u1"));
    }

    [Fact]
    public async Task UploadImageAsync_Png_StoresObjectAndReturnsDto()
    {
        await using var context = CreateInMemoryContext();
        var storage = new InMemoryMediaStorage();
        var svc = CreateService(context, storage);

        var dto = await svc.UploadImageAsync(new MemoryStream(PngHeader), "u1");

        Assert.Equal("image/png", dto.ContentType);
        Assert.Equal(PngHeader.Length, dto.SizeBytes);
        Assert.Equal($"/api/media/{dto.Id}", dto.Url);

        var asset = await context.MediaAssets.FindAsync(dto.Id);
        Assert.NotNull(asset);
        Assert.Equal("u1", asset!.UploaderId);
        Assert.EndsWith(".png", asset.ObjectKey);
        Assert.Equal(PngHeader, storage.Objects[asset.ObjectKey]);
    }

    [Fact]
    public async Task UploadImageAsync_ExceedsMaxBytes_ThrowsArgumentException()
    {
        await using var context = CreateInMemoryContext();
        var storage = new InMemoryMediaStorage();
        var svc = CreateService(context, storage, maxBytes: 16);

        var oversized = new byte[17];
        PngHeader.CopyTo(oversized, 0);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.UploadImageAsync(new MemoryStream(oversized), "u1"));
        Assert.Empty(storage.Objects);
    }

    [Fact]
    public async Task UploadImageAsync_Svg_ThrowsArgumentException()
    {
        await using var context = CreateInMemoryContext();
        var storage = new InMemoryMediaStorage();
        var svc = CreateService(context, storage);

        var svg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");

        await Assert.ThrowsAsync<ArgumentException>(() => svc.UploadImageAsync(new MemoryStream(svg), "u1"));
        Assert.Empty(storage.Objects);
        Assert.Empty(context.MediaAssets);
    }

    [Fact]
    public async Task UploadImageAsync_Empty_ThrowsArgumentException()
    {
        await using var context = CreateInMemoryContext();
        var svc = CreateService(context, new InMemoryMediaStorage());

        await Assert.ThrowsAsync<ArgumentException>(() => svc.UploadImageAsync(new MemoryStream(), "u1"));
    }

    [Fact]
    public async Task OpenAsync_ReturnsStoredContentAndType()
    {
        await using var context = CreateInMemoryContext();
        var svc = CreateService(context, new InMemoryMediaStorage());
        var dto = await svc.UploadImageAsync(new MemoryStream(PngHeader), "u1");

        var (content, contentType) = await svc.OpenAsync(dto.Id);

        Assert.Equal("image/png", contentType);
        using var read = new MemoryStream();
        await content.CopyToAsync(read);
        Assert.Equal(PngHeader, read.ToArray());
    }

    [Fact]
    public async Task OpenAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = CreateService(context, new InMemoryMediaStorage());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.OpenAsync(Guid.NewGuid()));
    }
}
