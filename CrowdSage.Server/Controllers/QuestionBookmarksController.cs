using System.Security.Claims;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers;

[Route("api/question/bookmark")]
[ApiController]
public class QuestionBookmarksController(IQuestionsService questionsService, ILogger<QuestionBookmarksController> logger) : ControllerBase
{
    [Authorize]
    [HttpGet()]
    public async Task<IActionResult> GetBookmarkedQuestions()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookmarkedQuestions = await questionsService.GetBookmarkedQuestions(userId!);
            return Ok(bookmarkedQuestions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching bookmarked questions.");
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}