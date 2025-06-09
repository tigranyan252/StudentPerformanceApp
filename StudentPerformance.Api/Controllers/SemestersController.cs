// Path: StudentPerformance.Api/Controllers/SemestersController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For StatusCodes
using System;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces; // Для интерфейсов ISemesterService, IUserService
using static StudentPerformance.Api.Utilities.UserRoles; // Для констант ролей
using StudentPerformance.Api.Exceptions; // Для NotFoundException и ConflictException
using Microsoft.Extensions.Logging; // Для логирования

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Все действия в этом контроллере требуют аутентификации по умолчанию
    public class SemestersController : ControllerBase
    {
        // Инжектируем специализированные сервисы
        private readonly ISemesterService _semesterService;
        private readonly IUserService _userService; // Все еще нужен для получения UserType, если он не из клейма
        private readonly ILogger<SemestersController> _logger;

        // Конструктор контроллера
        public SemestersController(ISemesterService semesterService, IUserService userService, ILogger<SemestersController> logger)
        {
            _semesterService = semesterService;
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
        /// Gets a list of all semesters with optional filtering by name, code, start date, and end date.
        /// Requires Administrator, Teacher, or Student roles, and fine-grained permission.
        /// </summary>
        /// <param name="name">Optional: Filter semesters by name.</param>
        /// <param name="code">Optional: Filter semesters by code.</param>
        /// <param name="startDateFrom">Optional: Filter semesters starting from this date.</param>
        /// <param name="endDateTo">Optional: Filter semesters ending by this date.</param>
        /// <returns>A list of Semester DTOs.</returns>
        [HttpGet]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        // ИЗМЕНЕНО: Добавлены все параметры фильтрации
        public async Task<ActionResult<IEnumerable<SemesterDto>>> GetAllSemesters(
            [FromQuery] string? name = null,
            [FromQuery] string? code = null,
            [FromQuery] DateTime? startDateFrom = null,
            [FromQuery] DateTime? endDateTo = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Использование ISemesterService для проверки авторизации
                bool authorized = await _semesterService.CanUserViewAllSemestersAsync(currentUserId);
                if (!authorized)
                {
                    _logger.LogWarning("GetAllSemesters: User {UserId} is not authorized to view all semesters.", currentUserId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для просмотра всех семестров." });
                }

                // ИЗМЕНЕНО: Передача всех параметров в сервисный слой
                var semesters = await _semesterService.GetAllSemestersAsync(name, code, startDateFrom, endDateTo);
                return Ok(semesters);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to GetAllSemesters.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all semesters.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Произошла ошибка при получении списка семестров." });
            }
        }

        [HttpGet("{semesterId}")]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SemesterDto>> GetSemesterById(int semesterId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Использование ISemesterService для проверки авторизации
                bool authorized = await _semesterService.CanUserViewSemesterDetailsAsync(currentUserId, semesterId);
                if (!authorized)
                {
                    _logger.LogWarning("GetSemesterById: User {UserId} is not authorized to view semester {SemesterId}.", currentUserId, semesterId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для просмотра деталей этого семестра." });
                }

                var semester = await _semesterService.GetSemesterByIdAsync(semesterId);

                if (semester == null)
                {
                    return NotFound($"Семестр с ID {semesterId} не найден.");
                }

                return Ok(semester);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to GetSemesterById for semester {SemesterId}.", semesterId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "GetSemesterById: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching semester {SemesterId} by ID.", semesterId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Произошла ошибка при получении семестра с ID {semesterId}." });
            }
        }

        [HttpPost]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddSemester([FromBody] AddSemesterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();

                // Использование ISemesterService для проверки авторизации
                bool authorized = await _semesterService.CanUserManageSemestersAsync(currentUserId);
                if (!authorized)
                {
                    _logger.LogWarning("AddSemester: User {UserId} is not authorized to add semesters.", currentUserId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для добавления семестров." });
                }

                var addedSemesterDto = await _semesterService.AddSemesterAsync(request);

                if (addedSemesterDto == null)
                {
                    return BadRequest("Failed to add semester due to an unexpected reason.");
                }

                return CreatedAtAction(nameof(GetSemesterById), new { semesterId = addedSemesterDto.SemesterId }, addedSemesterDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "AddSemester: Invalid argument provided: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "AddSemester: Conflict detected: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to AddSemester.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a semester.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Произошла ошибка при добавлении семестра." });
            }
        }

        [HttpPut("{semesterId}")]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateSemester(int semesterId, [FromBody] UpdateSemesterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();

                // Использование ISemesterService для проверки авторизации
                bool authorized = await _semesterService.CanUserManageSemestersAsync(currentUserId);
                if (!authorized)
                {
                    _logger.LogWarning("UpdateSemester: User {UserId} is not authorized to update semester {SemesterId}.", currentUserId, semesterId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для обновления семестров." });
                }

                var isUpdated = await _semesterService.UpdateSemesterAsync(semesterId, request);

                if (!isUpdated)
                {
                    // Это маловероятно, так как UpdateSemesterAsync должен выбрасывать исключения при Not Found
                    var existingSemester = await _semesterService.GetSemesterByIdAsync(semesterId);
                    if (existingSemester == null)
                    {
                        return NotFound($"Семестр с ID {semesterId} не найден.");
                    }
                    return BadRequest("Failed to update semester. Check for data conflicts.");
                }

                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "UpdateSemester: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "UpdateSemester: Conflict detected: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to UpdateSemester for semester {SemesterId}.", semesterId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating semester with ID: {SemesterId}", semesterId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Произошла ошибка при обновлении семестра с ID {semesterId}." });
            }
        }

        [HttpDelete("{semesterId}")]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteSemester(int semesterId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Использование ISemesterService для проверки авторизации
                bool authorized = await _semesterService.CanUserManageSemestersAsync(currentUserId);
                if (!authorized)
                {
                    _logger.LogWarning("DeleteSemester: User {UserId} is not authorized to delete semester {SemesterId}.", currentUserId, semesterId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "У вас нет прав для удаления семестров." });
                }

                await _semesterService.DeleteSemesterAsync(semesterId);
                _logger.LogInformation($"DeleteSemester: Semester {semesterId} deleted successfully via controller.");
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "DeleteSemester: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "DeleteSemester: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to DeleteSemester for semester {SemesterId}.", semesterId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting semester with ID: {SemesterId}", semesterId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Произошла неожиданная ошибка при удалении семестра с ID {semesterId}." });
            }
        }
    }
}
