using CrowdSage.Server.Models;
using CrowdSage.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController(QuestionsService questionsService) : ControllerBase
    {
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
        public async Task<IActionResult> AddQuestionAsync([FromBody] Question question)
        {
            try
            {
                await questionsService.AddQuestionAsync(question);
                return CreatedAtAction(nameof(GetAction), new { id = question.Id }, question);
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
        public async Task<IActionResult> EditQuestion(Guid id, [FromBody] Question question)
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
