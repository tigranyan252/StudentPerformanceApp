using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services.Interfaces; // Используем интерфейс IUserService
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System;
using StudentPerformance.Api.Models.Requests;
using Microsoft.Extensions.Logging;
using StudentPerformance.Api.Exceptions; // Убедитесь, что это правильный путь к вашим кастомным исключениям
using static StudentPerformance.Api.Utilities.UserRoles; // Для прямого доступа к константам ролей
using System.ComponentModel.DataAnnotations; // Для ValidationException

// Удаляем эту строку, так как она не нужна, если UserRoles используется через static using
// using StudentPerformance.Api.Services;

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Базовый маршрут: /api/Auth
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user with a username and password, returning a JWT token upon success.
        /// </summary>
        /// <param name="model">The login request containing username and password.</param>
        /// <returns>
        ///   <see cref="StatusCodes.Status200OK"/> with <see cref="AuthenticationResult"/> if authentication is successful.
        ///   <see cref="StatusCodes.Status401Unauthorized"/> if authentication fails (invalid credentials).
        ///   <see cref="StatusCodes.Status400BadRequest"/> if the request model is invalid.
        ///   <see cref="StatusCodes.Status500InternalServerError"/> for unexpected errors.
        /// </returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            _logger.LogInformation("Login attempt for username: {Username}", model.Username); // ИСПРАВЛЕНО: model.Login -> model.Username

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed: Invalid model state for username {Username}.", model.Username);
                return BadRequest(ModelState);
            }

            try
            {
                // ИСПРАВЛЕНО: Метод AuthenticateUserAsync теперь выбрасывает исключение для неверных учетных данных
                var result = await _userService.AuthenticateUserAsync(model.Username, model.Password); // ИСПРАВЛЕНО: model.Login -> model.Username

                // Если AuthenticateUserAsync вернул null или result.Success=false, это означает,
                // что он не выбросил исключение, но аутентификация все равно не удалась.
                // В нашей текущей реализации UserService, AuthenticateUserAsync выбрасывает исключение,
                // поэтому этот блок if (result == null || !result.Success) может быть избыточным,
                // но оставлен как защитный механизм.
                if (result == null || !result.Success)
                {
                    _logger.LogWarning("Authentication failed for user {Username}: Invalid credentials (returned null/false from service).", model.Username);
                    return Unauthorized(new { message = "Invalid credentials." });
                }

                _logger.LogInformation("User {Username} authenticated successfully.", model.Username);
                return Ok(result);
            }
            catch (ArgumentException ex) // Catch specific ArgumentException from UserService for invalid credentials
            {
                _logger.LogWarning(ex, "Authentication failed for user {Username}: {Message}", model.Username, ex.Message);
                return Unauthorized(new { message = ex.Message }); // Возвращаем 401 для неверных учетных данных
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during login for user {Username}.", model.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred during login." });
            }
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="model">The registration request containing user details.</param>
        /// <returns>
        ///   <see cref="StatusCodes.Status201Created"/> if registration is successful.
        ///   <see cref="StatusCodes.Status400BadRequest"/> if the request model is invalid or role/group not found.
        ///   <see cref="StatusCodes.Status409Conflict"/> if username is already taken.
        ///   <see cref="StatusCodes.Status500InternalServerError"/> for unexpected errors.
        /// </returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            _logger.LogInformation("Registration attempt for username: {Username}, user type: {UserType}", model.Username, model.UserType);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Registration failed: Invalid model state for username {Username}.", model.Username);
                return BadRequest(ModelState); // Возвращает ошибки валидации из атрибутов DTO
            }

            try
            {
                var newUserDto = await _userService.RegisterUserAsync(model);

                _logger.LogInformation("New user {Username} registered successfully with ID {UserId}.", newUserDto.Username, newUserDto.UserId);

                // CreatedAtAction указывает на метод GetUserById в UsersController
                return CreatedAtAction(
                    actionName: nameof(UsersController.GetUserById), // Метод в UsersController
                    controllerName: "Users", // Имя контроллера (без "Controller" суффикса)
                    routeValues: new { userId = newUserDto.UserId }, // Параметр маршрута
                    value: newUserDto // Возвращаемое значение
                );
            }
            catch (ConflictException ex) // Пользователь с таким именем уже существует
            {
                _logger.LogWarning(ex, "Registration failed (Conflict): {Message}", ex.Message);
                return Conflict(new { message = ex.Message }); // 409 Conflict
            }
            catch (ArgumentException ex) // Неверный UserType или GroupId не найден (или другие аргументы)
            {
                _logger.LogWarning(ex, "Registration failed (Bad Request - Argument): {Message}", ex.Message);
                return BadRequest(new { message = ex.Message }); // 400 Bad Request
            }
            catch (ValidationException ex) // Ошибки валидации из сервиса (если сервис выбрасывает ValidationException)
            {
                _logger.LogWarning(ex, "Registration validation failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) // Общая обработка непредвиденных ошибок
            {
                _logger.LogError(ex, "An unexpected error occurred during user registration for {Username}.", model.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        /// <summary>
        /// Changes the password for the currently authenticated user.
        /// </summary>
        /// <param name="request">Request containing old and new passwords.</param>
        /// <returns>NoContent if successful, BadRequest if invalid, Unauthorized if old password doesn't match.</returns>
        [HttpPut("change-password")]
        [Authorize] // Только аутентифицированные пользователи могут менять свой пароль
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Если пользователь не найден
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Для concurrency issues
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            _logger.LogInformation("Change password attempt for current user.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Change password failed: Invalid model state.");
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogError("Current user ID claim (NameIdentifier) is missing or invalid from token during password change.");
                return Unauthorized("Invalid user ID in token.");
            }

            try
            {
                // Метод сервиса теперь выбрасывает исключения при неудаче
                await _userService.ChangePasswordAsync(userId, request);

                _logger.LogInformation("Password successfully changed for user {UserId}.", userId);
                return NoContent(); // 204 No Content, если операция успешна и нет содержимого для возврата
            }
            catch (ArgumentException ex) // Например, "Invalid old password."
            {
                _logger.LogWarning(ex, "Password change failed for user {UserId}: {Message}", userId, ex.Message);
                return BadRequest(new { message = ex.Message }); // 400 Bad Request
            }
            catch (NotFoundException ex) // Если пользователь не найден в сервисе
            {
                _logger.LogWarning(ex, "Password change failed for user {UserId}: {Message}", userId, ex.Message);
                return NotFound(new { message = ex.Message }); // 404 Not Found
            }
            catch (ConflictException ex) // Для DbUpdateConcurrencyException, если сервис перевыбрасывает как ConflictException
            {
                _logger.LogWarning(ex, "Password change failed for user {UserId} (Conflict): {Message}", userId, ex.Message);
                return Conflict(new { message = ex.Message }); // 409 Conflict
            }
            catch (Exception ex) // Общая обработка непредвиденных ошибок
            {
                _logger.LogError(ex, "An unexpected error occurred while changing password for user {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }
    }
}
