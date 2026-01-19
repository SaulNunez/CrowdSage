using CrowdSage.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class AccountController(UserManager<CrowdsageUser> _userManager) : Controller
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] Register model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            user = new CrowdsageUser { UserName = model.UserName, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return Ok();
            }
        }

        // If we got this far, something failed.
        return BadRequest(ModelState);
    }
}