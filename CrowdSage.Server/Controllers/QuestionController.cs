using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrowdSage.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult GetAction(string id)
        {
            return Ok("This is a placeholder for the QuestionController action.");
        }
    }
}
