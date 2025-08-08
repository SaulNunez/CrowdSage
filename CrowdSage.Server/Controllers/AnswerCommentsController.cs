using CrowdSage.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CrowdSage.Server.Services;
using System.Threading.Tasks;

namespace CrowdSage.Server.Controllers;

[Route("api/questions/{questionId}/answers/{answerId}/comments")]
[ApiController]
public class AnswerCommentsController(AnswerCommentService answerCommentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] AnswerComment comment, Guid answerId)
    {
        if (comment == null || string.IsNullOrWhiteSpace(comment.Content))
        {
            return new BadRequestObjectResult("Comment text cannot be empty.");
        }
        try
        {
            comment.CreatedAt = DateTime.UtcNow;
            await answerCommentService.AddCommentAsync(comment, answerId);
            return new CreatedAtActionResult(nameof(CreateComment), nameof(AnswerCommentsController), new { id = comment.Id }, comment);
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

    [HttpGet("{answerId}")]
    public async Task<IActionResult> GetCommentsForAnswer(Guid answerId)
    {
        try
        {
            var comments = await answerCommentService.GetCommentsForAnswer(answerId);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditComment(Guid id, [FromBody] AnswerComment updated)
    {
        if (updated == null || string.IsNullOrWhiteSpace(updated.Content))
        {
            return BadRequest("Comment text cannot be empty.");
        }
        try
        {
            await answerCommentService.EditCommentAsync(id, updated);
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
            await answerCommentService.DeleteCommentAsync(id);
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