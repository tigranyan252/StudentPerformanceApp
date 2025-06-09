// Path: StudentPerformance.Api/Controllers/AssignmentsController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services.Interfaces; // Для нового IAssignmentService
using StudentPerformance.Api.Models.DTOs; // Для AssignmentDto
using StudentPerformance.Api.Models.Requests; // Для запросов Add/Update
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Для StatusCodes
using System; // Для исключений
using System.Security.Claims; // Для GetCurrentUserId
using static StudentPerformance.Api.Utilities.UserRoles; // Для констант ролей

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // МАРШРУТ БУДЕТ /api/Assignments
    [Authorize] // Защищаем контроллер
    public class AssignmentsController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService; // НОВЫЙ сервис для общих заданий
        private readonly IUserService _userService; // Для авторизации
        private readonly ILogger<AssignmentsController> _logger;

        public AssignmentsController(IAssignmentService assignmentService, IUserService userService, ILogger<AssignmentsController> logger)
        {
            _assignmentService = assignmentService;
            _userService = userService;
            _logger = logger;
        }

        // Вспомогательный метод для получения ID текущего пользователя
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User ID claim not found or invalid in token.");
        }

        /// <summary>
        /// Получает список всех общих заданий (Homework, Projects).
        /// Доступно Администраторам и Преподавателям.
        /// </summary>
        /// <returns>Список AssignmentDto.</returns>
        [HttpGet] // Отвечает на GET /api/Assignments
        [Authorize(Roles = $"{Administrator},{Teacher}")] // Разрешаем доступ Admin и Teacher
        [ProducesResponseType(typeof(IEnumerable<AssignmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetAllAssignments()
        {
            _logger.LogInformation("Attempting to get all general assignments.");
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = await _userService.GetUserRoleAsync(currentUserId);

                // Здесь можно добавить более тонкую логику авторизации,
                // например, учитель видит только задания по своим курсам.
                // Но для простоты пока разрешаем всем учителям и админам видеть все задания.
                if (currentUserRole != Administrator && currentUserRole != Teacher)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to view general assignments." });
                }

                var assignments = await _assignmentService.GetAllAssignmentsAsync();
                return Ok(assignments);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to get all general assignments.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all general assignments.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        /// <summary>
        /// Получает общее задание по ID.
        /// Доступно Администраторам и Преподавателям (если имеют права).
        /// </summary>
        /// <param name="assignmentId">ID задания.</param>
        /// <returns>AssignmentDto или NotFound.</returns>
        [HttpGet("{assignmentId}")]
        [Authorize(Roles = $"{Administrator},{Teacher}")]
        [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AssignmentDto>> GetAssignmentById(int assignmentId)
        {
            _logger.LogInformation("Attempting to get general assignment with ID: {AssignmentId}", assignmentId);
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = await _userService.GetUserRoleAsync(currentUserId);

                // Можно добавить проверку _userService.CanUserViewAssignmentDetailsAsync(currentUserId, assignmentId);
                // если нужны более тонкие права доступа, например, учитель может видеть только свои задания.

                var assignmentDto = await _assignmentService.GetAssignmentByIdAsync(assignmentId);

                if (assignmentDto == null)
                {
                    _logger.LogWarning("General assignment with ID {AssignmentId} not found.", assignmentId);
                    return NotFound($"General assignment with ID {assignmentId} not found.");
                }

                return Ok(assignmentDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to get general assignment {AssignmentId}.", assignmentId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving general assignment {AssignmentId}.", assignmentId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        /// <summary>
        /// Добавляет новое общее задание.
        /// Доступно Администраторам.
        /// </summary>
        /// <param name="request">Данные нового задания.</param>
        /// <returns>Созданный AssignmentDto.</returns>
        [HttpPost]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddAssignment([FromBody] AddAssignmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Add assignment failed: Invalid model state.");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Attempting to add new general assignment: {Title}", request.Title);
            try
            {
                var currentUserId = GetCurrentUserId();
                // Можно добавить проверку _userService.CanUserAddAssignmentAsync(currentUserId);
                // если нужны более тонкие права доступа.

                var newAssignment = await _assignmentService.AddAssignmentAsync(request);
                return CreatedAtAction(nameof(GetAssignmentById), new { assignmentId = newAssignment!.AssignmentId }, newAssignment);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Add assignment failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to add general assignment.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding new general assignment.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        /// <summary>
        /// Обновляет существующее общее задание.
        /// Доступно Администраторам.
        /// </summary>
        /// <param name="assignmentId">ID задания для обновления.</param>
        /// <param name="request">Данные для обновления.</param>
        /// <returns>NoContent или NotFound/BadRequest.</returns>
        [HttpPut("{assignmentId}")]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAssignment(int assignmentId, [FromBody] UpdateAssignmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Update assignment {AssignmentId} failed: Invalid model state.", assignmentId);
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Attempting to update general assignment with ID: {AssignmentId}", assignmentId);
            try
            {
                var currentUserId = GetCurrentUserId();
                // Можно добавить проверку _userService.CanUserUpdateAssignmentAsync(currentUserId, assignmentId);
                // если нужны более тонкие права доступа.

                var isUpdated = await _assignmentService.UpdateAssignmentAsync(assignmentId, request);
                if (!isUpdated)
                {
                    _logger.LogWarning("Update for general assignment {AssignmentId} failed: not found or no changes.", assignmentId);
                    return NotFound($"General assignment with ID {assignmentId} not found or no changes were made.");
                }
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Update assignment {AssignmentId} failed: {Message}", assignmentId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to update general assignment {AssignmentId}.", assignmentId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating general assignment {AssignmentId}.", assignmentId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        /// <summary>
        /// Удаляет общее задание.
        /// Доступно Администраторам.
        /// </summary>
        /// <param name="assignmentId">ID задания для удаления.</param>
        /// <returns>NoContent или NotFound/Conflict.</returns>
        [HttpDelete("{assignmentId}")]
        [Authorize(Roles = Administrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Для внешних ключей
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAssignment(int assignmentId)
        {
            _logger.LogInformation("Attempting to delete general assignment with ID: {AssignmentId}", assignmentId);
            try
            {
                var currentUserId = GetCurrentUserId();
                // Можно добавить проверку _userService.CanUserDeleteAssignmentAsync(currentUserId, assignmentId);
                // если нужны более тонкие права доступа.

                var isDeleted = await _assignmentService.DeleteAssignmentAsync(assignmentId);
                if (!isDeleted)
                {
                    _logger.LogWarning("Delete for general assignment {AssignmentId} failed: not found.", assignmentId);
                    return NotFound($"General assignment with ID {assignmentId} not found.");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex) // От сервиса, если есть зависимости (Restrict)
            {
                _logger.LogWarning(ex, "Delete for general assignment {AssignmentId} failed due to dependencies.", assignmentId);
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to delete general assignment {AssignmentId}.", assignmentId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting general assignment {AssignmentId}.", assignmentId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }
    }
}
