using System.Security.Claims;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers;

[Route("api/question/bookmark")]
[ApiController]
public class QuestionBookmarksController(IQuestionsService questionsService) : ControllerBase
{
    [Authorize]
    [HttpGet()]
    public IActionResult GetBookmarkedQuestions()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookmarkedQuestions = questionsService.GetBookmarkedQuestions(userId!);
            return Ok(bookmarkedQuestions);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}