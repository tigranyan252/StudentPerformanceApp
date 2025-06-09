// Path: StudentPerformance.Api/Controllers/TeachersController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces;
using System;
using Microsoft.Extensions.Logging; // Добавлено для ILogger
using static StudentPerformance.Api.Utilities.UserRoles; // Для прямого доступа к константам ролей
using StudentPerformance.Api.Exceptions; // Добавлено для использования кастомных исключений

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Применяем авторизацию ко всему контроллеру, чтобы требовать токен
    public class TeachersController : ControllerBase
    {
        private readonly ITeacherService _teacherService;
        private readonly IUserService _userService; // Нужен для централизованных проверок прав
        private readonly ILogger<TeachersController> _logger; // Добавлено поле для логирования

        public TeachersController(ITeacherService teacherService, IUserService userService, ILogger<TeachersController> logger)
        {
            _teacherService = teacherService;
            _userService = userService;
            _logger = logger; // Инициализация логгера
        }

        // Вспомогательный метод для получения ID текущего пользователя из Claims Principal
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            // Если ID пользователя не может быть извлечен из токена, это ошибка авторизации.
            _logger.LogWarning("User ID claim not found or is invalid in the authentication token.");
            throw new UnauthorizedAccessException("User ID claim not found or is invalid in the authentication token.");
        }


        [HttpGet]
        [Authorize(Roles = $"{Administrator},{Teacher}")] // Доступно администратору и преподавателю
        public async Task<ActionResult<IEnumerable<TeacherDto>>> GetAllTeachers(
            [FromQuery] string? userName) // Убран groupId из контроллера для согласованности с фронтендом
        {
            try
            {
                int currentUserId = GetCurrentUserId();

                // Проверка авторизации делегирована сервису IUserService
                // Примечание: ITeacherService уже вызывает _userService.CanUserViewAllTeachersAsync(currentUserId)
                // Этот контроллер-уровень Authorization attribute (Roles = ...) также проверяет это.
                // Тем не менее, явная проверка здесь полезна для раннего выхода и более специфичных сообщений об ошибке.
                if (!await _userService.CanUserViewAllTeachersAsync(currentUserId))
                {
                    // Возвращаем 403 Forbidden, если пользователь не авторизован
                    _logger.LogWarning("User {UserId} is not authorized to view all teachers.", currentUserId);
                    return StatusCode(403, new { message = "You are not authorized to view all teachers." });
                }

                // Передаем currentUserId в сервис
                var teachers = await _teacherService.GetAllTeachersAsync(userName, currentUserId);
                return Ok(teachers);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in GetAllTeachers by user ID from token.");
                return Forbid(ex.Message); // HTTP 403 Forbidden
            }
            catch (Exception ex)
            {
                // Логирование ошибки с помощью ILogger
                _logger.LogError(ex, "An unexpected error occurred in GetAllTeachers.");
                return StatusCode(500, new { message = "An unexpected error occurred while fetching teachers.", detail = ex.Message });
            }
        }

        [HttpGet("{teacherId}")]
        [Authorize(Roles = $"{Administrator},{Teacher}")] // Доступно администратору и преподавателю
        public async Task<ActionResult<TeacherDto>> GetTeacherById(int teacherId)
        {
            try
            {
                int currentUserId = GetCurrentUserId();

                // Проверка авторизации делегирована сервису IUserService
                if (!await _userService.CanUserViewTeacherDetailsAsync(currentUserId, teacherId))
                {
                    _logger.LogWarning("User {UserId} is not authorized to view teacher details for teacher ID {TeacherId}.", currentUserId, teacherId);
                    return StatusCode(403, new { message = "You are not authorized to view this teacher's details." });
                }

                // Передаем currentUserId в сервис
                var teacher = await _teacherService.GetTeacherByIdAsync(teacherId, currentUserId);
                if (teacher == null)
                {
                    _logger.LogWarning("Teacher with ID {TeacherId} not found.", teacherId);
                    return NotFound();
                }

                return Ok(teacher);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in GetTeacherById by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Teacher details not found for teacher ID {TeacherId}.", teacherId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in GetTeacherById for teacher ID {TeacherId}.", teacherId);
                return StatusCode(500, new { message = "An unexpected error occurred while fetching teacher details.", detail = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = Administrator)] // Только администратор может добавлять преподавателей
        public async Task<IActionResult> AddTeacher([FromBody] AddTeacherRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid ModelState for AddTeacher request: {ModelStateErrors}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                int currentUserId = GetCurrentUserId();

                // Проверка авторизации делегирована сервису IUserService
                if (!await _userService.CanUserAddTeacherAsync(currentUserId))
                {
                    _logger.LogWarning("User {UserId} is not authorized to add teachers.", currentUserId);
                    return StatusCode(403, new { message = "You are not authorized to add teachers." });
                }

                // Передаем currentUserId в сервис
                var addedTeacherDto = await _teacherService.AddTeacherAsync(request, currentUserId);

                // Эта ветка, скорее всего, не будет достигнута, так как сервис выбрасывает исключение при ошибках.
                // Она остается как запасной вариант, если сервис вернет null без выброса исключения.
                if (addedTeacherDto == null)
                {
                    _logger.LogError("TeacherService.AddTeacherAsync returned null unexpectedly for request: {@Request}", request);
                    return BadRequest(new { message = "Failed to add teacher due to invalid data." });
                }

                return CreatedAtAction(nameof(GetTeacherById), new { teacherId = addedTeacherDto.TeacherId }, addedTeacherDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in AddTeacher by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (ConflictException ex) // For username already taken, etc.
            {
                _logger.LogWarning(ex, "Conflict occurred while adding teacher: {Message}", ex.Message);
                return Conflict(new { message = ex.Message }); // HTTP 409 Conflict
            }
            catch (NotFoundException ex) // For related entity not found (e.g., Role)
            {
                _logger.LogWarning(ex, "Dependent entity not found for AddTeacher request: {Message}", ex.Message);
                return NotFound(new { message = ex.Message }); // HTTP 404 Not Found
            }
            catch (ArgumentException ex) // For invalid arguments passed to service
            {
                _logger.LogWarning(ex, "Invalid argument provided for AddTeacher request: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) // For other specific business logic errors
            {
                _logger.LogError(ex, "Invalid operation occurred during AddTeacher: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while adding teacher.");
                return StatusCode(500, new { message = "An unexpected error occurred while adding teacher.", detail = ex.Message });
            }
        }

        [HttpPut("{teacherId}")]
        [Authorize(Roles = Administrator)] // Только администратор может обновлять преподавателей
        public async Task<IActionResult> UpdateTeacher(int teacherId, [FromBody] UpdateTeacherRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid ModelState for UpdateTeacher request (Teacher ID: {TeacherId}): {ModelStateErrors}", teacherId, ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                int currentUserId = GetCurrentUserId();

                // Проверка авторизации делегирована сервису IUserService
                if (!await _userService.CanUserUpdateTeacherAsync(currentUserId, teacherId))
                {
                    _logger.LogWarning("User {UserId} is not authorized to update teacher ID {TeacherId}.", currentUserId, teacherId);
                    return StatusCode(403, new { message = "You are not authorized to update this teacher." });
                }

                // Передаем currentUserId в сервис
                var isUpdated = await _teacherService.UpdateTeacherAsync(teacherId, request, currentUserId);

                if (!isUpdated)
                {
                    _logger.LogWarning("UpdateTeacherAsync returned false for teacher ID {TeacherId}. Teacher not found or update failed.", teacherId);
                    return NotFound(); // Если сервис вернул false (например, teacherId не найден)
                }

                _logger.LogInformation("Teacher {TeacherId} updated successfully by user {UserId}.", teacherId, currentUserId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in UpdateTeacher by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (NotFoundException ex) // Catch specific Not Found from service
            {
                _logger.LogWarning(ex, "Teacher not found for update (Teacher ID: {TeacherId}): {Message}", teacherId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex) // Catch specific Conflict from service (e.g., username already taken)
            {
                _logger.LogWarning(ex, "Conflict occurred while updating teacher (Teacher ID: {TeacherId}): {Message}", teacherId, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex) // For invalid arguments passed to service
            {
                _logger.LogWarning(ex, "Invalid argument provided for UpdateTeacher request (Teacher ID: {TeacherId}): {Message}", teacherId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Использование ILogger для безопасного логирования исключения
                _logger.LogError(ex, "An unexpected error occurred while updating teacher with ID {TeacherId}.", teacherId);
                return StatusCode(500, new { message = "An unexpected error occurred while updating teacher.", detail = ex.Message });
            }
        }

        [HttpDelete("{teacherId}")]
        [Authorize(Roles = Administrator)] // Только администратор может удалять преподавателей
        public async Task<IActionResult> DeleteTeacher(int teacherId)
        {
            try
            {
                int currentUserId = GetCurrentUserId();

                // Проверка авторизации делегирована сервису IUserService
                if (!await _userService.CanUserDeleteTeacherAsync(currentUserId, teacherId))
                {
                    _logger.LogWarning("User {UserId} is not authorized to delete teacher ID {TeacherId}.", currentUserId, teacherId);
                    return StatusCode(403, new { message = "You are not authorized to delete teachers." });
                }

                // Передаем currentUserId в сервис
                var isDeleted = await _teacherService.DeleteTeacherAsync(teacherId, currentUserId);

                if (!isDeleted)
                {
                    _logger.LogWarning("TeacherService.DeleteTeacherAsync returned false for teacher ID {TeacherId}. Teacher not found or delete failed.", teacherId);
                    return NotFound(); // Если сервис вернул false (например, teacherId не найден)
                }

                _logger.LogInformation("Teacher {TeacherId} deleted successfully by user {UserId}.", teacherId, currentUserId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in DeleteTeacher by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (NotFoundException ex) // Catch specific Not Found from service
            {
                _logger.LogWarning(ex, "Teacher not found for deletion (Teacher ID: {TeacherId}): {Message}", teacherId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex) // Catch specific Conflict from service (e.g., associated data)
            {
                _logger.LogWarning(ex, "Conflict occurred while deleting teacher (Teacher ID: {TeacherId}): {Message}", teacherId, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting teacher with ID {TeacherId}.", teacherId);
                return StatusCode(500, new { message = "An unexpected error occurred while deleting teacher.", detail = ex.Message });
            }
        }
    }
}
