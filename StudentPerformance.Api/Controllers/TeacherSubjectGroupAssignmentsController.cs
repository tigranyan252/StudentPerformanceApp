// Path: StudentPerformance.Api/Controllers/TeacherSubjectGroupAssignmentsController.cs

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging; // Добавлено для ILogger
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces; // Используем интерфейс ITeacherSubjectGroupAssignmentService
using static StudentPerformance.Api.Utilities.UserRoles;
using StudentPerformance.Api.Exceptions; // Добавлено для NotFoundException, ConflictException

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    // ИСПРАВЛЕНО: Маршрут остается, но имя контроллера теперь точно отражает его назначение
    [Route("api/teacher-subject-group-assignments")]
    [Authorize] // Применяем авторизацию глобально к контроллеру
    // ИСПРАВЛЕНО: Переименовано из AssignmentsController
    public class TeacherSubjectGroupAssignmentsController : ControllerBase
    {
        private readonly IUserService _userService;
        // ИСПРАВЛЕНО: Используем новый интерфейс ITeacherSubjectGroupAssignmentService
        private readonly ITeacherSubjectGroupAssignmentService _teacherSubjectGroupAssignmentService;
        private readonly ILogger<TeacherSubjectGroupAssignmentsController> _logger; // Добавлено поле для логирования

        // ИСПРАВЛЕНО: Обновлен конструктор с добавлением ILogger
        public TeacherSubjectGroupAssignmentsController(
            IUserService userService,
            ITeacherSubjectGroupAssignmentService teacherSubjectGroupAssignmentService,
            ILogger<TeacherSubjectGroupAssignmentsController> logger)
        {
            _userService = userService;
            _teacherSubjectGroupAssignmentService = teacherSubjectGroupAssignmentService;
            _logger = logger; // Инициализация логгера
        }

        // Вспомогательный метод для получения ID пользователя из Claims
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            _logger.LogWarning("User ID claim not found or invalid in token. Throwing UnauthorizedAccessException.");
            throw new UnauthorizedAccessException("User ID claim not found or invalid in token. Please log in again.");
        }

        /// <summary>
        /// Adds a new teacher-subject-group assignment.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="request">The assignment data.</param>
        /// <returns>A DTO of the newly created assignment or BadRequest if invalid.</returns>
        [HttpPost]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Добавлено для ConflictException
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Добавлено для NotFoundException
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Добавлено для общих ошибок
        public async Task<IActionResult> AddTeacherSubjectGroupAssignment([FromBody] AddTeacherSubjectGroupAssignmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid ModelState for AddTeacherSubjectGroupAssignment request: {@ModelStateErrors}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();

                // Эта проверка CanUserAddAssignmentAsync избыточна, если она просто дублирует [Authorize(Roles = Administrator)].
                // Если у вас есть более сложная логика авторизации (например, проверка прав на основе данных, а не только роли),
                // то оставьте ее. В противном случае ее можно удалить.
                if (!await _userService.CanUserAddAssignmentAsync(currentUserId))
                {
                    _logger.LogWarning("User {UserId} is not authorized to add assignments.", currentUserId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to add assignments." });
                }

                // ИСПРАВЛЕНО: Вызов метода нового сервиса
                var assignmentDto = await _teacherSubjectGroupAssignmentService.AddAssignmentAsync(request);

                if (assignmentDto == null)
                {
                    // Эта ветка, скорее всего, не будет достигнута, так как сервис выбрасывает исключение
                    _logger.LogError("TeacherSubjectGroupAssignmentService.AddAssignmentAsync returned null unexpectedly for request: {@Request}", request);
                    return BadRequest("Failed to add assignment. It might already exist or related entities are invalid.");
                }

                _logger.LogInformation("Assignment {AssignmentId} added successfully by user {UserId}.", assignmentDto.TeacherSubjectGroupAssignmentId, currentUserId);
                return CreatedAtAction(
                    nameof(GetTeacherSubjectGroupAssignmentById),
                    new { assignmentId = assignmentDto.TeacherSubjectGroupAssignmentId },
                    assignmentDto
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in AddTeacherSubjectGroupAssignment by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (ConflictException ex) // Для конфликтов, таких как уже существующее назначение
            {
                _logger.LogWarning(ex, "Conflict occurred while adding assignment: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (NotFoundException ex) // Для отсутствующих связанных сущностей (учитель, предмет, группа, семестр)
            {
                _logger.LogWarning(ex, "Related entity not found for AddTeacherSubjectGroupAssignment request: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex) // Для некорректных аргументов
            {
                _logger.LogWarning(ex, "Invalid argument provided for AddTeacherSubjectGroupAssignment request: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) // Общий перехват для любых других неожиданных ошибок
            {
                _logger.LogError(ex, "An unexpected error occurred while adding teacher subject group assignment.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while adding assignment.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific teacher-subject-group assignment by its ID.
        /// Requires Administrator or Teacher (if assigned) roles.
        /// </summary>
        /// <param name="assignmentId">The ID of the assignment.</param>
        /// <returns>The assignment DTO or NotFound.</returns>
        [HttpGet("{assignmentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Добавлено для общих ошибок
        public async Task<IActionResult> GetTeacherSubjectGroupAssignmentById(int assignmentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Эта проверка должна быть более сложной, если учитель может видеть только свои назначения.
                // Предполагается, что CanUserViewAssignmentDetailsAsync охватывает эту сложную логику.
                if (!await _userService.CanUserViewAssignmentDetailsAsync(currentUserId, assignmentId))
                {
                    _logger.LogWarning("User {UserId} is not authorized to view assignment details for ID {AssignmentId}.", currentUserId, assignmentId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to view this assignment." });
                }

                // ИСПРАВЛЕНО: Вызов метода нового сервиса
                var assignmentDto = await _teacherSubjectGroupAssignmentService.GetAssignmentByIdAsync(assignmentId);

                if (assignmentDto == null)
                {
                    _logger.LogWarning("Assignment with ID {AssignmentId} not found.", assignmentId);
                    return NotFound($"Assignment with ID {assignmentId} not found.");
                }

                _logger.LogInformation("Assignment {AssignmentId} retrieved by user {UserId}.", assignmentId, currentUserId);
                return Ok(assignmentDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in GetTeacherSubjectGroupAssignmentById by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (NotFoundException ex) // Для отсутствующих назначений (хотя уже есть проверка null)
            {
                _logger.LogWarning(ex, "Assignment not found for ID {AssignmentId}: {Message}", assignmentId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching assignment with ID {AssignmentId}.", assignmentId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching assignment details.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets all teacher-subject-group assignments.
        /// Requires Administrator or Teacher roles.
        /// </summary>
        /// <returns>A list of assignment DTOs.</returns>
        [HttpGet] // Этот метод для получения всех (или фильтрованных) НАЗНАЧЕНИЙ
        [Authorize(Roles = $"{Administrator},{Teacher}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Добавлено для общих ошибок
        public async Task<IActionResult> GetAllTeacherSubjectGroupAssignments()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = await _userService.GetUserRoleAsync(currentUserId);

                IEnumerable<object> assignments;

                if (currentUserRole == Administrator)
                {
                    // Администратор видит все назначения
                    assignments = await _teacherSubjectGroupAssignmentService.GetAllAssignmentsAsync();
                    _logger.LogInformation("Admin user {UserId} retrieved all {Count} assignments.", currentUserId, assignments.Count());
                }
                else if (currentUserRole == Teacher)
                {
                    // Учитель видит только свои назначения
                    var teacherProfile = await _userService.GetTeacherByIdAsync(currentUserId);
                    if (teacherProfile == null)
                    {
                        _logger.LogWarning("Teacher profile not found for user {UserId} attempting to view assignments.", currentUserId);
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Teacher profile not found for this user." });
                    }
                    assignments = await _teacherSubjectGroupAssignmentService.GetAssignmentsForTeacherAsync(teacherProfile.TeacherId);
                    _logger.LogInformation("Teacher user {UserId} retrieved {Count} of their assignments.", currentUserId, assignments.Count());
                }
                else
                {
                    _logger.LogWarning("User {UserId} with role {Role} is not authorized to view assignments.", currentUserId, currentUserRole);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to view assignments." });
                }

                return Ok(assignments);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in GetAllTeacherSubjectGroupAssignments by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching all teacher subject group assignments.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching assignments.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing teacher-subject-group assignment.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="assignmentId">The ID of the assignment to update.</param>
        /// <param name="request">The updated data for the assignment.</param>
        /// <returns>NoContent if successful, NotFound or BadRequest otherwise.</returns>
        [HttpPut("{assignmentId}")]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Добавлено для ConflictException
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Добавлено для общих ошибок
        public async Task<IActionResult> UpdateTeacherSubjectGroupAssignment(int assignmentId, [FromBody] UpdateTeacherSubjectGroupAssignmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid ModelState for UpdateTeacherSubjectGroupAssignment request (Assignment ID: {AssignmentId}): {@ModelStateErrors}", assignmentId, ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                if (!await _userService.CanUserUpdateAssignmentAsync(currentUserId, assignmentId))
                {
                    _logger.LogWarning("User {UserId} is not authorized to update assignment ID {AssignmentId}.", currentUserId, assignmentId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to update this assignment." });
                }

                var isUpdated = await _teacherSubjectGroupAssignmentService.UpdateAssignmentAsync(assignmentId, request);

                if (isUpdated)
                {
                    _logger.LogInformation("Assignment {AssignmentId} updated successfully by user {UserId}.", assignmentId, currentUserId);
                    return NoContent();
                }

                // Эта ветка должна быть достигнута только если сервис вернул false, но не бросил исключение,
                // что обычно означает, что сущность не была найдена или была удалена другим процессом.
                _logger.LogWarning("Assignment with ID {AssignmentId} not found or update failed unexpectedly (service returned false).", assignmentId);
                return NotFound($"Assignment with ID {assignmentId} not found or update failed due to data conflict (e.g., duplicate assignment after changes).");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in UpdateTeacherSubjectGroupAssignment by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (NotFoundException ex) // Для отсутствующего назначения
            {
                _logger.LogWarning(ex, "Assignment not found for update (Assignment ID: {AssignmentId}): {Message}", assignmentId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex) // Для конфликтов, таких как уже существующее назначение после обновления
            {
                _logger.LogWarning(ex, "Conflict occurred while updating assignment (Assignment ID: {AssignmentId}): {Message}", assignmentId, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex) // Для некорректных аргументов
            {
                _logger.LogWarning(ex, "Invalid argument provided for UpdateTeacherSubjectGroupAssignment request (Assignment ID: {AssignmentId}): {Message}", assignmentId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating teacher subject group assignment with ID {AssignmentId}.", assignmentId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while updating assignment.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a teacher-subject-group assignment.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="assignmentId">The ID of the assignment to delete.</param>
        /// <returns>NoContent if successful, NotFound otherwise.</returns>
        [HttpDelete("{assignmentId}")]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Добавлено для ConflictException
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Добавлено для общих ошибок
        public async Task<IActionResult> DeleteTeacherSubjectGroupAssignment(int assignmentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!await _userService.CanUserDeleteAssignmentAsync(currentUserId, assignmentId))
                {
                    _logger.LogWarning("User {UserId} is not authorized to delete assignment ID {AssignmentId}.", currentUserId, assignmentId);
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to delete this assignment." });
                }

                var isDeleted = await _teacherSubjectGroupAssignmentService.DeleteAssignmentAsync(assignmentId);

                if (isDeleted)
                {
                    _logger.LogInformation("Assignment {AssignmentId} deleted successfully by user {UserId}.", assignmentId, currentUserId);
                    return NoContent();
                }

                _logger.LogWarning("Assignment with ID {AssignmentId} not found for deletion (service returned false).", assignmentId);
                return NotFound($"Assignment with ID {assignmentId} not found.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in DeleteTeacherSubjectGroupAssignment by user ID from token.");
                return Forbid(ex.Message);
            }
            catch (NotFoundException ex) // Для отсутствующего назначения
            {
                _logger.LogWarning(ex, "Assignment not found for deletion (Assignment ID: {AssignmentId}): {Message}", assignmentId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex) // Для конфликтов зависимостей (например, связанные оценки)
            {
                _logger.LogWarning(ex, "Conflict occurred while deleting assignment (Assignment ID: {AssignmentId}): {Message}", assignmentId, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting teacher subject group assignment with ID {AssignmentId}.", assignmentId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while deleting assignment.", detail = ex.Message });
            }
        }
    }
}
