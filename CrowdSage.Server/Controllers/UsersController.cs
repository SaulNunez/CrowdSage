using System.Security.Claims;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController(IUserProfileService userProfileService, ILogger<UsersController> logger) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await GetUserProfile(userId!);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserProfile(string userId)
    {
        try
        {
            var profile = await userProfileService.GetProfileAsync(userId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {userId} not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching profile for user {userId}.", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [HttpGet("{userId}/questions")]
    public Task<IActionResult> GetUserQuestions(string userId, [FromQuery] int take = 20, [FromQuery] int page = 1) =>
        GetPage(userId, take, page, "questions", userProfileService.GetQuestionsAsync);

    [HttpGet("{userId}/answers")]
    public Task<IActionResult> GetUserAnswers(string userId, [FromQuery] int take = 20, [FromQuery] int page = 1) =>
        GetPage(userId, take, page, "answers", userProfileService.GetAnswersAsync);

    [HttpGet("{userId}/comments")]
    public Task<IActionResult> GetUserComments(string userId, [FromQuery] int take = 20, [FromQuery] int page = 1) =>
        GetPage(userId, take, page, "comments", userProfileService.GetCommentsAsync);

    private async Task<IActionResult> GetPage<T>(string userId, int take, int page, string kind, Func<string, int, int, Task<List<T>>> fetch)
    {
        if (take <= 0 || page <= 0)
        {
            return BadRequest("Results per page and page number must be greater than zero.");
        }

        try
        {
            var items = await fetch(userId, take, (page - 1) * take);
            return Ok(items);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {userId} not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching {kind} for user {userId}.", kind, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}
