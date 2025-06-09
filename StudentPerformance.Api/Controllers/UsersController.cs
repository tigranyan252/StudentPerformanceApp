// Path: StudentPerformance.Api/Controllers/UsersController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For StatusCodes
using System;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces; // Для интерфейсов IUserService, IJwtService
using static StudentPerformance.Api.Utilities.UserRoles; // Для констант ролей
using Microsoft.Extensions.Logging; // Для логирования
using StudentPerformance.Api.Exceptions; // Для NotFoundException, ConflictException, BadRequestException

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Все действия в этом контроллере требуют аутентификации по умолчанию
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // Вспомогательный метод для получения ID текущего пользователя из claims
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            _logger.LogWarning("GetCurrentUserId: User ID claim not found or invalid in token.");
            throw new UnauthorizedAccessException("User ID claim not found or invalid in token.");
        }

        /// <summary>
        /// Gets a list of all users with optional filtering by username and user type.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="username">Optional: Filter users by username.</param>
        /// <param name="userType">Optional: Filter users by user type (role name).</param>
        /// <returns>A list of User DTOs.</returns>
        [HttpGet]
        [Authorize(Roles = Administrator)] // Только администраторы могут просматривать всех пользователей
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        // ИЗМЕНЕНО: Добавлены параметры username и userType с [FromQuery]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers([FromQuery] string? username, [FromQuery] string? userType)
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserViewAllUsersAsync(currentUserId);

                if (!authorized)
                {
                    _logger.LogWarning("GetAllUsers: User {UserId} is not authorized to view all users.", currentUserId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для просмотра всех пользователей." });
                }

                // ИЗМЕНЕНО: Передаем параметры в сервис
                var userDtos = await _userService.GetAllUsersAsync(username, userType);
                return Ok(userDtos);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to GetAllUsers.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all users.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Произошла ошибка при получении списка пользователей." });
            }
        }

        /// <summary>
        /// Gets a specific user by ID.
        /// Requires Administrator role or the user to be the owner of the profile.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The User DTO or NotFound if the user does not exist.</returns>
        [HttpGet("{userId}")]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")] // Администратор, учитель, студент могут просматривать свой профиль
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserDto>> GetUserById(int userId)
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserViewUserDetailsAsync(currentUserId, userId);

                if (!authorized)
                {
                    _logger.LogWarning("GetUserById: User {CurrentUserId} is not authorized to view user {TargetUserId}.", currentUserId, userId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для просмотра деталей этого пользователя." });
                }

                var user = await _userService.GetUserByIdAsync(userId);
                // NotFoundException теперь выбрасывается сервисом, если пользователь не найден
                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to GetUserById for user {UserId}.", userId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "GetUserById: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Произошла ошибка при получении пользователя с ID {userId}." });
            }
        }

        /// <summary>
        /// Registers a new user. Available to unauthenticated users or admins creating new accounts.
        /// </summary>
        /// <param name="request">Registration data including username, password, email, and user type.</param>
        /// <returns>The newly created User DTO.</returns>
        [HttpPost("register")]
        [AllowAnonymous] // Разрешаем всем регистрироваться
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Для случая, когда роль не найдена
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var newUser = await _userService.RegisterUserAsync(request);
                return CreatedAtAction(nameof(GetUserById), new { userId = newUser?.UserId }, newUser);
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Registration conflict: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (NotFoundException ex) // Для случая, когда роль не найдена
            {
                _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user registration.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Произошла ошибка при регистрации пользователя." });
            }
        }

        /// <summary>
        /// Adds a new user by administrator.
        /// </summary>
        /// <param name="request">Add user data including username, password, email, user type, first name, last name, and optional group ID.</param>
        /// <returns>The newly created User DTO.</returns>
        [HttpPost] // Использование того же роута, но с авторизацией
        [Authorize(Roles = Administrator)] // Только администраторы могут добавлять пользователей через этот эндпоинт
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> AddUser([FromBody] RegisterRequest request) // Используем RegisterRequest, так как он содержит все необходимые поля
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                int currentUserId = GetCurrentUserId();
                // Проверка, что текущий пользователь может добавлять пользователей (т.е. он админ)
                bool authorized = await _userService.CanUserManageUsersAsync(currentUserId);
                if (!authorized)
                {
                    _logger.LogWarning("AddUser: User {UserId} is not authorized to add users.", currentUserId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для добавления пользователей." });
                }

                var newUser = await _userService.RegisterUserAsync(request); // Переиспользуем логику регистрации
                return CreatedAtAction(nameof(GetUserById), new { userId = newUser?.UserId }, newUser);
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "AddUser conflict: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "AddUser failed: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to AddUser.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during adding a user by admin.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Произошла ошибка при добавлении пользователя." });
            }
        }


        /// <summary>
        /// Updates an existing user.
        /// Requires Administrator role or the user to be the owner of the profile.
        /// </summary>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="request">The updated data for the user.</param>
        /// <returns>NoContent if successful, NotFound if the user doesn't exist, or BadRequest for invalid data.</returns>
        [HttpPut("{userId}")]
        [Authorize(Roles = Administrator)] // Только администраторы могут обновлять других пользователей
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserUpdateUserAsync(currentUserId, userId);

                if (!authorized)
                {
                    _logger.LogWarning("UpdateUser: User {CurrentUserId} is not authorized to update user {TargetUserId}.", currentUserId, userId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для обновления этого пользователя." });
                }

                bool isUpdated = await _userService.UpdateUserAsync(userId, request);
                if (!isUpdated)
                {
                    // Если сервис возвращает false, но не выбрасывает NotFoundException, это может быть 
                    // логическая ошибка или отсутствие изменений.
                    return BadRequest("Failed to update user. Check for data conflicts or invalid data.");
                }
                _logger.LogInformation("User {UserId} updated successfully by user {CurrentUserId}.", userId, currentUserId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "UpdateUser: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "UpdateUser conflict: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (BadRequestException ex) // Для ошибок вроде неверного текущего пароля
            {
                _logger.LogWarning(ex, "UpdateUser bad request: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to UpdateUser for user {UserId}.", userId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Произошла ошибка при обновлении пользователя с ID {userId}." });
            }
        }

        /// <summary>
        /// Changes the password for the current authenticated user.
        /// </summary>
        /// <param name="request">Request containing current and new passwords.</param>
        /// <returns>NoContent if successful.</returns>
        [HttpPut("change-password")]
        [Authorize] // Любой аутентифицированный пользователь может сменить свой пароль
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Если пользователь не найден (хотя должен быть авторизован)
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                int currentUserId = GetCurrentUserId();
                bool isChanged = await _userService.ChangePasswordAsync(currentUserId, request);
                if (!isChanged)
                {
                    // Этот случай должен быть перехвачен сервисом как NotFoundException или BadRequestException
                    return BadRequest("Failed to change password.");
                }
                _logger.LogInformation("Password changed successfully for user {CurrentUserId}.", currentUserId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "ChangePassword: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (BadRequestException ex) // Для неверного текущего пароля
            {
                _logger.LogWarning(ex, "ChangePassword bad request: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to ChangePassword.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing password.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Произошла ошибка при смене пароля." });
            }
        }


        /// <summary>
        /// Deletes a user.
        /// Requires Administrator role and fine-grained permission (cannot delete self if sole admin).
        /// </summary>
        /// <param name="userId">The ID of the user to delete.</param>
        /// <returns>NoContent if successful, NotFound if the user does not exist, or Conflict if dependencies exist.</returns>
        [HttpDelete("{userId}")]
        [Authorize(Roles = Administrator)] // Только администраторы могут удалять пользователей
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserDeleteUserAsync(currentUserId, userId);

                if (!authorized)
                {
                    _logger.LogWarning("DeleteUser: User {CurrentUserId} is not authorized to delete user {TargetUserId}.", currentUserId, userId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для удаления этого пользователя." });
                }

                // Дополнительная проверка на фронтенде/сервисе, чтобы не удалить себя, если админ
                if (currentUserId == userId)
                {
                    _logger.LogWarning("DeleteUser: User {CurrentUserId} attempted to delete their own account.", currentUserId);
                    return BadRequest(new { message = "Вы не можете удалить свою собственную учетную запись администратора через этот эндпоинт." });
                }

                await _userService.DeleteUserAsync(userId); // Сервис сам выбрасывает исключения
                _logger.LogInformation("User {UserId} deleted successfully by user {CurrentUserId}.", userId, currentUserId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "DeleteUser: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "DeleteUser conflict: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to DeleteUser for user {UserId}.", userId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting user {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Произошла ошибка при удалении пользователя с ID {userId}." });
            }
        }
    }
}
