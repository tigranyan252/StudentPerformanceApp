// Path: StudentPerformance.Api/Controllers/SubjectsController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces; // ИСПРАВЛЕНО: Используем интерфейсы
using System; // ДОБАВЛЕНО: Для UnauthorizedAccessException, ArgumentException, InvalidOperationException
using static StudentPerformance.Api.Utilities.UserRoles; // ДОБАВЛЕНО: Для доступа к константам ролей

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Авторизация на уровне контроллера для всех методов по умолчанию
    public class SubjectsController : ControllerBase
    {
        private readonly ISubjectService _subjectService;
        private readonly IUserService _userService; // ДОБАВЛЕНО: Для общих проверок авторизации

        public SubjectsController(ISubjectService subjectService, IUserService userService) // ДОБАВЛЕНО: Инъекция IUserService
        {
            _subjectService = subjectService;
            _userService = userService; // Инициализация
        }

        // Вспомогательный метод для получения ID текущего пользователя
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User ID claim not found or is invalid.");
        }

        [HttpGet]
        // ИСПРАВЛЕНО: Использование констант ролей из UserRoles
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")]
        public async Task<ActionResult<List<SubjectDto>>> GetAllSubjects(
            [FromQuery] string? name, // ДОБАВЛЕНО: Параметр для фильтрации по имени
            [FromQuery] string? code) // ДОБАВЛЕНО: Параметр для фильтрации по коду
        {
            int currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserViewAllSubjectsAsync(currentUserId);
            if (!authorized)
            {
                // ИСПРАВЛЕНО: Возвращаем StatusCode(403) с сообщением для согласованности
                return StatusCode(403, new { message = "You are not authorized to view all subjects." });
            }

            // ИСПРАВЛЕНО: Передаем параметры name и code в сервис
            var subjects = await _subjectService.GetAllSubjectsAsync(name, code);
            return Ok(subjects);
        }

        [HttpGet("{subjectId}")]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")]
        public async Task<ActionResult<SubjectDto>> GetSubjectById(int subjectId)
        {
            int currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserViewSubjectDetailsAsync(currentUserId, subjectId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to view this subject's details." });
            }

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
            {
                return NotFound($"Subject with ID {subjectId} not found."); // Уточненное сообщение
            }

            return Ok(subject);
        }

        [HttpPost]
        [Authorize(Roles = Administrator)] // ИСПРАВЛЕНО: Использование константы роли
        public async Task<IActionResult> AddSubject([FromBody] AddSubjectRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserAddSubjectAsync(currentUserId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to add subjects." });
            }

            try
            {
                // ИСПРАВЛЕНО: Сервис теперь выбрасывает исключение, а не возвращает null
                var addedSubjectDto = await _subjectService.AddSubjectAsync(request);
                return CreatedAtAction(nameof(GetSubjectById), new { subjectId = addedSubjectDto.SubjectId }, addedSubjectDto);
            }
            catch (ArgumentException ex) // ДОБАВЛЕНО: Обработка ArgumentException (например, для дубликатов)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{subjectId}")]
        [Authorize(Roles = Administrator)] // ИСПРАВЛЕНО: Использование константы роли
        public async Task<IActionResult> UpdateSubject(int subjectId, [FromBody] UpdateSubjectRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserUpdateSubjectAsync(currentUserId, subjectId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to update this subject." });
            }

            try
            {
                var isUpdated = await _subjectService.UpdateSubjectAsync(subjectId, request);
                if (!isUpdated)
                {
                    // ИСПРАВЛЕНО: Уточненное сообщение для NotFound
                    return NotFound($"Subject with ID {subjectId} not found or no changes were made.");
                }
                return NoContent();
            }
            catch (ArgumentException ex) // ДОБАВЛЕНО: Обработка ArgumentException (например, для дубликатов)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{subjectId}")]
        [Authorize(Roles = Administrator)] // ИСПРАВЛЕНО: Использование константы роли
        public async Task<IActionResult> DeleteSubject(int subjectId)
        {
            int currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserDeleteSubjectAsync(currentUserId, subjectId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to delete subjects." });
            }

            try
            {
                var isDeleted = await _subjectService.DeleteSubjectAsync(subjectId);
                if (!isDeleted)
                {
                    return NotFound($"Subject with ID {subjectId} not found."); // Уточненное сообщение
                }
                return NoContent();
            }
            catch (InvalidOperationException ex) // ДОБАВЛЕНО: Обработка InvalidOperationException (для зависимостей)
            {
                return Conflict(new { message = ex.Message }); // 409 Conflict для проблем с зависимостями
            }
        }
    }
}
