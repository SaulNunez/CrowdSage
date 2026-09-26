using System.Security.Claims;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers;

[Route("api/media")]
[ApiController]
public class MediaController(IMediaService mediaService, ILogger<MediaController> logger) : ControllerBase
{
    // Leaves headroom over MediaStorageOptions.MaxBytes for the multipart envelope;
    // the service enforces the actual image size limit.
    private const long MaxRequestBytes = 6 * 1024 * 1024;

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null)
        {
            return BadRequest("No file was uploaded.");
        }
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await using var stream = file.OpenReadStream();
            var media = await mediaService.UploadImageAsync(stream, userId!, cancellationToken);
            return CreatedAtAction(nameof(GetMedia), new { id = media.Id }, media);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading media.");
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    // Anonymous on purpose: <img> tags cannot send the bearer token.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMedia(Guid id)
    {
        try
        {
            var (content, contentType) = await mediaService.OpenAsync(id);
            Response.Headers.XContentTypeOptions = "nosniff";
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return File(content, contentType);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Media with ID {id} not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching media with ID {mediaId}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}
