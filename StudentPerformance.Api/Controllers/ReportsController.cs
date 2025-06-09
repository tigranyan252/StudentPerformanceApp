// StudentPerformance.Api/Controllers/ReportsController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using Microsoft.Extensions.Logging;
using static StudentPerformance.Api.Utilities.UserRoles; // Для констант ролей

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Маршрут будет /api/reports
    [Authorize] // Применяем авторизацию ко всему контроллеру
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService; // Для получения роли пользователя
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService reportService, IUserService userService, ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _userService = userService;
            _logger = logger;
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
        /// Generates a summary report of student grades, showing average grades per subject.
        /// Accessible by Admin, Teacher (for their assigned students/groups), and Student (for their own grades).
        /// </summary>
        /// <param name="studentId">Optional: Filter by a specific student ID.</param>
        /// <param name="groupId">Optional: Filter by a specific group ID.</param>
        /// <param name="semesterId">Optional: Filter by a specific semester ID.</param>
        /// <returns>A list of StudentGradesSummaryDto.</returns>
        [HttpGet("student-grades-summary")] // Маршрут будет /api/reports/student-grades-summary
        [ProducesResponseType(200, Type = typeof(IEnumerable<StudentGradesSummaryDto>))]
        [ProducesResponseType(401)] // Unauthorized
        [ProducesResponseType(403)] // Forbidden
        [ProducesResponseType(500)] // Internal Server Error
        // Разрешаем доступ Администратору, Преподавателю и Студенту
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")]
        public async Task<IActionResult> GetStudentGradesSummary(
            [FromQuery] int? studentId = null,
            [FromQuery] int? groupId = null,
            [FromQuery] int? semesterId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = await _userService.GetUserRoleAsync(currentUserId);

                _logger.LogInformation("Attempting to get student grades summary report by UserId: {CurrentUserId}, Role: {CurrentUserRole}", currentUserId, currentUserRole);

                // Вызываем сервис для генерации отчета. Сервис будет выполнять всю логику фильтрации и авторизации по ролям.
                var reportData = await _reportService.GetStudentGradesSummaryAsync(
                    studentId,
                    groupId,
                    semesterId,
                    currentUserId,
                    currentUserRole
                );

                _logger.LogInformation("Successfully retrieved student grades summary report with {Count} entries for UserId: {CurrentUserId}.", reportData.Count(), currentUserId);
                return Ok(reportData);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to student grades summary report by UserId: {CurrentUserId}: {Message}", GetCurrentUserId(), ex.Message);
                return Forbid(ex.Message); // Возвращает 403 Forbidden
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while generating student grades summary report for UserId: {CurrentUserId}.", GetCurrentUserId());
                return StatusCode(500, new { message = "An unexpected error occurred while generating the report.", detail = ex.Message });
            }
        }
    }
}
