using System.Security.Claims;
using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController(IQuestionsService questionsService) : ControllerBase
    {
        [HttpGet("/new")]
        public async Task<IActionResult> GetNewQuestions([FromQuery] int resultsPerPage = 10, [FromQuery] int page = 1)
        {
            if (resultsPerPage <= 0 || page <= 0)
            {
                return BadRequest("Results per page and page number must be greater than zero.");
            }

            try
            {
                var questions = await questionsService.GetNewQuestionsAsync(resultsPerPage, (page - 1) * resultsPerPage);
                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetAction(string id)
        {
            try
            {
                var question = questionsService.GetQuestionById(Guid.Parse(id));
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
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
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
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
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
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
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
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }
    }
}
