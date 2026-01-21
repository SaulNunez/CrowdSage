using System.Security.Claims;
using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController(IQuestionsService questionsService, ILogger<QuestionsController> logger) : ControllerBase
    {
        [HttpGet("new")]
        public async Task<IActionResult> GetNewQuestions([FromQuery] int take = 10, [FromQuery] int page = 1)
        {
            if (take <= 0 || page <= 0)
            {
                return BadRequest("Results per page and page number must be greater than zero.");
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var questions = await questionsService.GetNewQuestionsAsync(userId,take, (page - 1) * take);
                return Ok(questions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching new questions.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetAction(Guid id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var question = questionsService.GetQuestionById(id, userId);
                return Ok(question);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Question with ID {id} not found.");
            }
            catch (FormatException)
            {
                return BadRequest("Invalid ID format.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching question with ID {questionId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddQuestionAsync([FromBody] QuestionPayload question)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var questionEntity = await questionsService.AddQuestionAsync(question, userId);
                return CreatedAtAction(nameof(GetAction), new { id = questionEntity.Id }, question);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating question.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> EditQuestion(Guid id, [FromBody] QuestionPayload question)
        {
            try
            {
                await questionsService.EditQuestion(id, question);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Question with ID {id} not found.");
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error editing question with Id {questionId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await questionsService.DeleteQuestion(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Question with ID {id} not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting question with ID {id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("{questionId}/bookmark")]
        public IActionResult BookmarkQuestion(Guid questionId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                questionsService.BookmarkQuestion(questionId, userId!);
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error bookmarking question with ID {questionId}.", questionId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [Authorize]
        [HttpDelete("{questionId}/bookmark")]
        public IActionResult RemoveBookmarkFromQuestion(Guid questionId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                questionsService.RemoveBookmarkFromQuestion(questionId, userId!);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Bookmark for question with ID {questionId} not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing bookmark for question.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("{answerId}/vote")]
        public async Task<IActionResult> VoteOnQuestion(Guid answerId, [FromBody] VoteInput voteInput)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await questionsService.VoteOnQuestion(answerId, userId!, voteInput);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Question with ID {answerId} not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error voting for question with ID {answerId}.", answerId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }
    }
}
