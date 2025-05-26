// Path: Controllers/AuthController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims; // <--- ADDED THIS LINE for ClaimTypes
using System; // For InvalidOperationException if needed (though not directly in the part you sent)


// Note: No need for System.ComponentModel.DataAnnotations as [ApiController] handles validation
// Note: No need for System.Threading.Tasks as async/await implies it

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] is not applied at the controller level here, as login needs to be anonymous.
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService; // Injected as interface for better practice

        public AuthController(IUserService userService) // Constructor injects the IUserService interface
        {
            _userService = userService;
        }

        /// <summary>
        /// Authenticates a user with a username and password, returning a JWT token upon success.
        /// </summary>
        /// <param name="model">The login request containing username and password.</param>
        /// <returns>
        ///   <see cref="StatusCodes.Status200OK"/> with <see cref="AuthenticationResult"/> if authentication is successful.
        ///   <see cref="StatusCodes.Status401Unauthorized"/> if authentication fails (invalid credentials).
        ///   <see cref="StatusCodes.Status400BadRequest"/> if the request model is invalid.
        /// </returns>
        [HttpPost("login")]
        [AllowAnonymous] // Allows unauthenticated access to this specific endpoint
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Unauthorized response (e.g., bad credentials)
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Bad request (e.g., validation errors)
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            // [ApiController] attribute automatically handles ModelState validation.
            // If the 'model' is invalid (e.g., missing required fields),
            // ASP.NET Core will automatically return a 400 Bad Request.

            var result = await _userService.AuthenticateUserAsync(model.Login, model.Password);

            // Check if authentication failed.
            // 'result' could be null if the user was not found at all,
            // or 'result.Success' could be false if the password was incorrect or other specific failure.
            if (result == null || !result.Success)
            {
                // For security reasons, it's good practice not to differentiate between
                // "user not found" and "incorrect password" in the response to the client.
                return Unauthorized(); // Returns HTTP 401 Unauthorized
            }

            // Authentication successful.
            return Ok(result); // Returns HTTP 200 OK with the AuthenticationResult (UserDto + Token)
        }

        // --- Other authentication-related actions can be added here, e.g.: ---

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="model">The registration request containing user details.</param>
        /// <returns>
        ///   <see cref="StatusCodes.Status201Created"/> if registration is successful.
        ///   <see cref="StatusCodes.Status400BadRequest"/> if the request model is invalid or username is taken.
        /// </returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var newUser = await _userService.RegisterUserAsync(model);

            if (newUser == null)
            {
                return BadRequest("Username already exists or invalid role/group provided.");
            }

            // Optionally, you might want to automatically log in the user after registration
            // var authResult = await _userService.AuthenticateUserAsync(model.Username, model.Password);
            // return CreatedAtAction(nameof(Login), authResult);

            return CreatedAtAction(
                "GetUserById", // Assuming you have a GetUserById method in a UserController or similar
                new { id = newUser.Id },
                newUser
            );
        }

        /// <summary>
        /// Changes the password for the currently authenticated user.
        /// </summary>
        /// <param name="request">Request containing old and new passwords.</param>
        /// <returns>NoContent if successful, BadRequest if invalid, Unauthorized if old password doesn't match.</returns>
        [HttpPut("change-password")]
        [Authorize] // This action requires authentication
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // If user not found (e.g., token ID invalid)
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid user ID in token.");
            }

            // Assuming your ChangePasswordAsync can return false for invalid old password or not found
            var isChanged = await _userService.ChangePasswordAsync(userId, request);

            if (!isChanged)
            {
                // This might need more specific error handling from the service
                // to differentiate between 'invalid old password' and 'user not found'.
                // For simplicity, returning BadRequest for general failure or Unauthorized for password mismatch.
                // If ChangePasswordAsync truly returns false for invalid old password:
                return Unauthorized("Invalid old password or user not found.");
            }

            return NoContent(); // Password changed successfully
        }
    }
}