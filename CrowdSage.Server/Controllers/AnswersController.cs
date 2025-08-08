using CrowdSage.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CrowdSage.Server.Services;

namespace CrowdSage.Server.Controllers;

[Route("api/question/{questionId}/answer")]
[ApiController]
public class AnswersController(AnswersService answersService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAnswerAsync([FromBody] Answer answer, Guid questionId)
    {
        if (answer == null || string.IsNullOrWhiteSpace(answer.Content))
        {
            return new BadRequestObjectResult("Answer text cannot be empty.");
        }
        try
        {
            // Simulate adding the answer to a data store
            answer.Id = Guid.NewGuid();
            answer.CreatedAt = DateTime.UtcNow;
            answer.QuestionId = questionId;
            await answersService.AddAnswerAsync(answer);

            return new CreatedAtActionResult("GetAnswer", "Answer", new { id = answer.Id }, answer);
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
    public async Task<IActionResult> GetAnswersForQuestion(Guid questionId)
    {
        try
        {
            var answers = await answersService.GetAnswersForQuestion(questionId);
            return Ok(answers);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditAnswer(Guid id, [FromBody] Answer answer)
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
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }

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
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
}