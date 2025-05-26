using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services; // Assuming IUserService is in this namespace
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Exceptions; // Ensure this is the correct path to your exceptions
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging; // For logging
using System;
using System.ComponentModel.DataAnnotations; // For generic Exception

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Base route: /api/users
    [Authorize] // All actions require authentication by default
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger; // Added ILogger

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Helper method to safely get the current authenticated user's ID.
        /// Throws UnauthorizedException if ID is missing/invalid.
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogError("Current user ID claim (NameIdentifier) is missing or invalid from token.");
                throw new UnauthorizedException("User ID is missing from token.");
            }
            return userId;
        }

   

        // --- Action to get a user by their ID ---
        /// <summary>
        /// Retrieves a user's details by their ID. Accessible by administrators and the user themselves.
        /// </summary>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId(); // Will throw if user ID is missing
                var canView = await _userService.CanUserViewUserDetailsAsync(currentUserId, userId);

                if (!canView)
                {
                    _logger.LogWarning("User {CurrentUserId} forbidden from viewing user {TargetUserId} details.", currentUserId, userId);
                    return Forbid("You are not authorized to view this user's details."); // Returns 403 Forbidden with default challenge
                }

                var userDto = await _userService.GetUserByIdAsync(userId);
                if (userDto == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", userId);
                    return NotFound(new { message = $"User with ID {userId} not found." });
                }
                return Ok(userDto);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt for GetUserById.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user by ID {UserId}.", userId);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

      

        // --- Action to get a list of ALL users ---
        /// <summary>
        /// Retrieves a list of all users. Accessible only to Administrators.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Администратор")] // Enforce role-based authorization directly
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            try
            {
                // Authorization is handled by [Authorize(Roles = "Администратор")] attribute
                var userDtos = await _userService.GetAllUsersAsync();
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all users.");
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }


        // --- Action to get a list of users by role ---
        /// <summary>
        /// Retrieves a list of users filtered by role name. Accessible to Administrators and Teachers.
        /// </summary>
        [HttpGet("role/{roleName}")]
        [Authorize(Roles = "Администратор,Преподаватель")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByRole(string roleName)
        {
            try
            {
                var userDtos = await _userService.GetUsersByRoleAsync(roleName);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving users by role {RoleName}.", roleName);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }


        // --- New Action: Register/Add a new user ---
        /// <summary>
        /// Registers a new user account. Accessible to anyone.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous] // Allow unauthenticated users to register
        [ProducesResponseType(typeof(UserDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Returns validation errors from DTO attributes
            }

            try
            {
                var newUser = await _userService.RegisterUserAsync(request);
                _logger.LogInformation("New user {Username} registered successfully with ID {UserId}.", newUser.Username, newUser.Id);
                return CreatedAtAction(nameof(GetUserById), new { userId = newUser.Id }, newUser);
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
                return Conflict(new { message = ex.Message }); // User with this login already exists
            }
            catch (NotFoundException ex) // If role not found during registration (e.g., requested role doesn't exist)
            {
                _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Registration validation failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user registration for {Username}.", request.Username);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

  

        // --- New Action: Update an existing user ---
        /// <summary>
        /// Updates an existing user's details. Accessible by administrators (any user) or the user themselves.
        /// </summary>
        [HttpPut("{userId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var canUpdate = await _userService.CanUserUpdateUserAsync(currentUserId, userId);
                if (!canUpdate)
                {
                    _logger.LogWarning("User {CurrentUserId} forbidden from updating user {TargetUserId}.", currentUserId, userId);
                    return Forbid("You are not authorized to update this user's profile."); // Returns 403 Forbidden with default challenge
                }

                var success = await _userService.UpdateUserAsync(userId, request);
                if (!success)
                {
                    // This case should ideally be covered by NotFoundException from service.
                    // But if service returns false without throwing, this handles it.
                    _logger.LogWarning("Update for user {UserId} failed (user not found or no changes).", userId);
                    return NotFound(new { message = "User not found or no changes were made." });
                }
                _logger.LogInformation("User {UserId} updated successfully.", userId);
                return NoContent(); // 204 No Content for successful update
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogError(ex, "Unauthorized attempt to update user.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning(ex, "Forbidden attempt: {Message}", ex.Message);
                return StatusCode(403, new { message = ex.Message }); // Corrected: Return 403 with message
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Update user failed: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Update user failed: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Update user validation failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user {UserId}.", userId);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

     

        // --- New Action: Change User Password ---
        /// <summary>
        /// Changes a user's password. Accessible by administrators (any user) or the user themselves.
        /// </summary>
        [HttpPut("{userId}/change-password")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                // Direct authorization check as per your original code.
                // The service method should contain the core logic for security and actual password verification.
                if (currentUserId != userId && !User.IsInRole("Администратор"))
                {
                    _logger.LogWarning("User {CurrentUserId} forbidden from changing password for user {TargetUserId}.", currentUserId, userId);
                    return Forbid("You are not authorized to change this user's password.");
                }

                // The service method should handle password match, old password verification etc.
                var success = await _userService.ChangePasswordAsync(userId, request);
                if (!success)
                {
                    // This means the service returned false without throwing a specific exception.
                    // The service method should ideally throw specific exceptions for clarity (e.g., InvalidCredentialsException, NotFoundException).
                    _logger.LogWarning("Password change for user {UserId} failed for an unspecified reason.", userId);
                    return BadRequest(new { message = "Password change failed. Please check your current password." });
                }
                _logger.LogInformation("Password successfully changed for user {UserId}.", userId);
                return NoContent();
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogError(ex, "Unauthorized attempt to change password.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning(ex, "Forbidden attempt: {Message}", ex.Message);
                return StatusCode(403, new { message = ex.Message }); // Corrected: Return 403 with message
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Change password failed: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Change password validation failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing password for user {UserId}.", userId);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

   

        // --- New Action: Delete a user ---
        /// <summary>
        /// Deletes a user account. Accessible only to Administrators.
        /// </summary>
        [HttpDelete("{userId}")]
        [Authorize(Roles = "Администратор")] // Enforce role-based authorization directly
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                // Additional service-level authorization for finer control if needed,
                // e.g., to prevent an admin from deleting themselves.
                if (!await _userService.CanUserDeleteUserAsync(currentUserId, userId))
                {
                    _logger.LogWarning("User {CurrentUserId} forbidden from deleting user {TargetUserId}.", currentUserId, userId);
                    return Forbid("You are not authorized to delete this user.");
                }

                var success = await _userService.DeleteUserAsync(userId);
                if (!success)
                {
                    _logger.LogWarning("Delete operation for user {UserId} failed (user not found or dependencies exist).", userId);
                    return NotFound(new { message = "User not found or could not be deleted (e.g., has dependencies)." });
                }
                _logger.LogInformation("User {UserId} deleted successfully.", userId);
                return NoContent(); // 204 No Content for successful deletion
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogError(ex, "Unauthorized attempt to delete user.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning(ex, "Forbidden attempt: {Message}", ex.Message);
                return StatusCode(403, new { message = ex.Message }); // Corrected: Return 403 with message
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Delete user failed: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex) // If there are related entities preventing deletion
            {
                _logger.LogWarning(ex, "Delete user failed due to conflict: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting user {UserId}.", userId);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }



        // --- Authentication Action (Login) ---
        /// <summary>
        /// Authenticates a user and returns authentication details (e.g., JWT token).
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous] // Login does not require prior authentication
        [ProducesResponseType(typeof(AuthenticationResult), 200)] // Assuming AuthResponseDto contains token and user info
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var authResult = await _userService.AuthenticateUserAsync(request.Login, request.Password);
                if (authResult == null)
                {
                    _logger.LogWarning("Authentication failed for user {Login}: Invalid credentials.", request.Login);
                    return Unauthorized(new { message = "Invalid login credentials." }); // Return 401 Unauthorized for invalid credentials
                }
                _logger.LogInformation("User {Login} authenticated successfully.", request.Login);
                return Ok(authResult); // Return 200 OK with the authentication result
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Login validation failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for user {Login}.", request.Login);
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        /*
         * The GetUserWithRoleById was commented out because GetUserByIdAsync from UserService
         * already fetches UserDto which should include RoleName if your DTO mapping is set up correctly.
         * If your UserDto doesn't include RoleName and you specifically need it, you'd add:
         *
         /// <summary>
         /// Retrieves user details including their role. Use if GetUserById doesn't provide role name.
         /// </summary>
         [HttpGet("{userId}/with-role")]
         [ProducesResponseType(typeof(UserDto), 200)]
         [ProducesResponseType(401)]
         [ProducesResponseType(403)]
         [ProducesResponseType(404)]
         [ProducesResponseType(500)]
         public async Task<IActionResult> GetUserWithRoleById(int userId)
         {
             try
             {
                 var currentUserId = GetCurrentUserId();
                 // Example simplified authorization for this specific endpoint: Admin or self
                 if (currentUserId != userId && !User.IsInRole("Администратор"))
                 {
                     _logger.LogWarning("User {CurrentUserId} forbidden from viewing user {TargetUserId} with role details.", currentUserId, userId);
                     return Forbid("You are not authorized to view this user's details with role.");
                 }

                 var userDto = await _userService.GetUserByIdAsync(userId); // Assuming this DTO contains RoleName

                 if (userDto == null)
                 {
                     _logger.LogWarning("User with ID {UserId} not found for GetUserWithRoleById.", userId);
                     return NotFound(new { message = $"User with ID {userId} not found." });
                 }

                 // If RoleName isn't automatically populated by AutoMapper in UserDto when calling GetUserByIdAsync,
                 // you would need a method in your service to explicitly fetch it, or adjust your AutoMapper profile.
                 // Example: userDto.RoleName = await _userService.GetUserRoleAsync(userId);

                 return Ok(userDto);
             }
             catch (UnauthorizedException ex)
             {
                 _logger.LogError(ex, "Unauthorized access attempt for GetUserWithRoleById.");
                 return Unauthorized(new { message = ex.Message });
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "An error occurred while retrieving user with role by ID {UserId}.", userId);
                 return StatusCode(500, new { message = "An internal server error occurred." });
             }
         }
         */
    }
}