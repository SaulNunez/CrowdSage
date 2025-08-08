using CrowdSage.Server.Models;
using Microsoft.AspNetCore.Mvc;
using CrowdSage.Server.Services;

namespace CrowdSage.Server.Controllers;

[Route("api/question/{questionId}/comment")]
[ApiController]
public class QuestionCommentsController(QuestionCommentService questionCommentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] QuestionComment comment, Guid questionId)
    {
        if (comment == null || string.IsNullOrWhiteSpace(comment.Content))
        {
            return new BadRequestObjectResult("Comment text cannot be empty.");
        }
        try
        {
            // Simulate adding the comment to a data store
            comment.CreatedAt = DateTime.UtcNow;
            await questionCommentService.AddCommnentAsync(comment, questionId);
            return new CreatedAtActionResult(nameof(CreateComment), nameof(QuestionCommentsController), new { id = comment.Id }, comment);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [HttpGet("{questionId}")]
    public async Task<IActionResult> GetCommentsForQuestion(Guid questionId)
    {
        try
        {
            var comments = await questionCommentService.GetCommentsForQuestion(questionId);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditComment(Guid id, [FromBody] QuestionComment updated)
    {
        if (updated == null || string.IsNullOrWhiteSpace(updated.Content))
        {
            return BadRequest("Comment text cannot be empty.");
        }
        try
        {
            await questionCommentService.EditCommentAsync(id, updated);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        try
        {
            await questionCommentService.DeleteCommentAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}