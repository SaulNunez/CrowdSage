using CrowdSage.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CrowdSage.Server.Services;
using CrowdSage.Server.Models.InsertUpdate;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CrowdSage.Server.Controllers;

[Route("api/question/{questionId}/answers")]
[ApiController]
public class AnswersController(IAnswersService answersService, ILogger<AnswersController> logger) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateAnswerAsync([FromBody] AnswerPayload answer, Guid questionId)
    {
        if (answer == null || string.IsNullOrWhiteSpace(answer.Content))
        {
            return new BadRequestObjectResult("Answer text cannot be empty.");
        }
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var answerEntity = await answersService.AddAnswerAsync(answer, questionId, userId);
            return new CreatedAtActionResult("GetAnswer", "Answer", new { id = answerEntity.Id }, answer);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating answer for question with ID {questionId}.", questionId);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAnswersForQuestion(Guid questionId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var answers = await answersService.GetAnswersForQuestion(questionId, userId);
            return Ok(answers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching new answers for question with ID {questionId}.", questionId);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditAnswer(Guid id, [FromBody] AnswerPayload answer)
    {
        if (answer == null || string.IsNullOrWhiteSpace(answer.Content))
        {
            return BadRequest("Answer content cannot be empty.");
        }
        try
        {
            await answersService.EditAnswer(id, answer);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Answer with ID {id} not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error editing answer with ID {questionId}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnswer(Guid id)
    {
        try
        {
            await answersService.DeleteAnswer(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Answer with ID {id} not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting answer with id {questionId}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [Authorize]
    [HttpPost("{answerId}/bookmark")]
    public IActionResult BookmarkAnswer(Guid answerId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            answersService.BookmarkAnswer(answerId, userId!);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bookmarking answer with ID {answerId}.", answerId);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [Authorize]
    [HttpDelete("{answerId}/bookmark")]
    public IActionResult RemoveBookmarkFromAnswer(Guid answerId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            answersService.RemoveBookmarkFromAnswer(answerId, userId!);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Bookmark for question with ID {answerId} not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error voting on answers with ID {answerId}.", answerId);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [Authorize]
    [HttpPost("{answerId}/vote")]
    public async Task<IActionResult> VoteOnAnswer(Guid answerId, [FromBody] VoteInput voteInput)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await answersService.VoteOnAnswer(answerId, userId!, voteInput);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Answer with ID {answerId} not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error voting on answers with ID {answerId}.", answerId);
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}