using System.Security.Claims;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers;

[Route("api/answer/bookmark")]
[ApiController]
public class AnswerBookmarksController(IAnswersService answersService, ILogger<AnswerBookmarksController> logger) : ControllerBase
{
    [Authorize]
    [HttpGet()]
    public async Task<IActionResult> GetBookmarkedAnswers()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookmarkedQuestions = await answersService.GetBookmarkedAnswers(userId!);
            return Ok(bookmarkedQuestions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting bookmarked answers.");
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}