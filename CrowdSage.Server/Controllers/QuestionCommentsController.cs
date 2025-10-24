using CrowdSage.Server.Models;
using Microsoft.AspNetCore.Mvc;
using CrowdSage.Server.Services;
using CrowdSage.Server.Models.InsertUpdate;
using System.Security.Claims;

namespace CrowdSage.Server.Controllers;

[Route("api/question/{questionId}/comment")]
[ApiController]
public class QuestionCommentsController(IQuestionCommentService questionCommentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] QuestionCommentPayload comment, Guid questionId)
    {
        if (comment == null || string.IsNullOrWhiteSpace(comment.Content))
        {
            return new BadRequestObjectResult("Comment text cannot be empty.");
        }
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var questionCommentEntity = await questionCommentService.AddCommnentAsync(comment, questionId, userId);
            return new CreatedAtActionResult(nameof(CreateComment), nameof(QuestionCommentsController), new { id = questionCommentEntity.Id }, comment);
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

    [HttpGet]
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
    public async Task<IActionResult> EditComment(Guid id, [FromBody] QuestionCommentPayload updated)
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