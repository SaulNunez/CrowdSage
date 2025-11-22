using System.Security.Claims;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers;

[Route("api/question/bookmark")]
[ApiController]
public class AnswerBookmarksController(IAnswersService answersService) : ControllerBase
{
    [Authorize]
    [HttpGet()]
    public IActionResult GetBookmarkedAnswers()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookmarkedQuestions = answersService.GetBookmarkedAnswers(userId!);
            return Ok(bookmarkedQuestions);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}