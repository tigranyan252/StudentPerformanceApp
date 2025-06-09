// Path: StudentPerformance.Api/Controllers/GradesController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using Microsoft.Extensions.Logging;
using StudentPerformance.Api.Models.Requests;
using static StudentPerformance.Api.Utilities.UserRoles;
using StudentPerformance.Api.Services;

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GradesController : ControllerBase
    {
        private readonly IGradeService _gradeService;
        private readonly IUserService _userService;
        private readonly ILogger<GradesController> _logger;

        public GradesController(IGradeService gradeService, IUserService userService, ILogger<GradesController> logger)
        {
            _gradeService = gradeService;
            _userService = userService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            _logger.LogWarning("User ID claim not found or invalid in token. Throwing UnauthorizedAccessException.");
            throw new UnauthorizedAccessException("User ID claim not found or is invalid in authenticated user context.");
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(List<GradeDto>))]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAllGrades(
            [FromQuery] int? studentId = null,
            [FromQuery] int? teacherId = null, // Этот параметр будет игнорироваться для авторизованного учителя
            [FromQuery] int? subjectId = null,
            [FromQuery] int? semesterId = null)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = await _userService.GetUserRoleAsync(currentUserId);

            _logger.LogInformation("GetAllGrades requested by UserId: {CurrentUserId}, Role: {CurrentUserRole}", currentUserId, currentUserRole);
            _logger.LogInformation("Query parameters received: StudentId={StudentId}, TeacherId={TeacherId}, SubjectId={SubjectId}, SemesterId={SemesterId}", studentId, teacherId, subjectId, semesterId);

            if (currentUserRole == Administrator)
            {
                _logger.LogInformation("User is Administrator. Retrieving all grades with provided filters.");
                var grades = await _gradeService.GetAllGradesAsync(studentId, teacherId, subjectId, semesterId);
                return Ok(grades);
            }
            else if (currentUserRole == Teacher)
            {
                var teacherProfile = await _userService.GetTeacherByIdAsync(currentUserId);

                if (teacherProfile == null)
                {
                    _logger.LogWarning("Teacher profile not found for UserId: {CurrentUserId}. Denying access.", currentUserId);
                    return StatusCode(403, new { message = "Teacher profile not found for this user. Access denied." });
                }

                _logger.LogInformation("Authenticated Teacher Profile ID: {TeacherProfileId} (associated with UserId: {CurrentUserId})", teacherProfile.TeacherId, currentUserId);

                // УДАЛЕНО: Условие, которое сравнивало teacherId из запроса с teacherProfile.TeacherId
                // Это условие приводило к 403 Forbidden, когда фронтенд отправлял UserId вместо TeacherId.
                // Вместо этого, мы принудительно фильтруем по teacherProfile.TeacherId текущего авторизованного преподавателя.

                _logger.LogInformation("Retrieving grades for authenticated Teacher (TeacherId: {ActualTeacherId}) with StudentId: {StudentId}, SubjectId: {SubjectId}, SemesterId: {SemesterId}", teacherProfile.TeacherId, studentId, subjectId, semesterId);

                // Всегда используем teacherProfile.TeacherId для фильтрации, чтобы учитель видел ТОЛЬКО свои оценки.
                // Параметр teacherId из FromQuery игнорируется в этом случае для целей авторизации/фильтрации.
                var grades = await _gradeService.GetAllGradesAsync(studentId, teacherProfile.TeacherId, subjectId, semesterId);
                return Ok(grades);
            }
            else if (currentUserRole == Student)
            {
                var studentProfile = await _userService.GetStudentByIdAsync(currentUserId);

                if (studentProfile == null)
                {
                    _logger.LogWarning("Student profile not found for UserId: {CurrentUserId}. Denying access.", currentUserId);
                    return StatusCode(403, new { message = "Student profile not found for this user. Access denied." });
                }

                _logger.LogInformation("Authenticated Student Profile ID: {StudentProfileId} (associated with UserId: {CurrentUserId})", studentProfile.StudentId, currentUserId);

                if (studentId.HasValue && studentId.Value != studentProfile.StudentId)
                {
                    _logger.LogWarning("Access denied for Student (UserId: {CurrentUserId}, StudentProfileId: {ActualStudentId}): Requested grades for StudentId {RequestedStudentId} which does not match their own.", currentUserId, studentProfile.StudentId, studentId.Value);
                    return StatusCode(403, new { message = "Students are only allowed to view their own grades." });
                }

                _logger.LogInformation("Retrieving grades for authenticated Student (StudentId: {ActualStudentId}) with SubjectId: {SubjectId}, SemesterId: {SemesterId}", studentProfile.StudentId, subjectId, semesterId);
                var grades = await _gradeService.GetAllGradesAsync(studentProfile.StudentId, null, subjectId, semesterId);
                return Ok(grades);
            }

            _logger.LogWarning("Access denied for UserId: {CurrentUserId} with unknown or unauthorized role {CurrentUserRole}.", currentUserId, currentUserRole);
            return StatusCode(403, new { message = "Access denied for this role." });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200, Type = typeof(GradeDto))]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGradeById(int id)
        {
            var currentUserId = GetCurrentUserId();
            var isAuthorized = await _userService.CanUserViewGradeDetailsAsync(currentUserId, id);

            if (!isAuthorized)
            {
                _logger.LogWarning("User {CurrentUserId} is not authorized to view grade ID {GradeId}.", currentUserId, id);
                return StatusCode(403, new { message = "You are not authorized to view this grade." });
            }

            var grade = await _gradeService.GetGradeByIdAsync(id);
            if (grade == null)
            {
                _logger.LogWarning("Grade with ID {GradeId} not found.", id);
                return NotFound($"Grade with ID {id} not found.");
            }

            _logger.LogInformation("Grade ID {GradeId} retrieved by user {CurrentUserId}.", id, currentUserId);
            return Ok(grade);
        }

        [HttpPost]
        [ProducesResponseType(201, Type = typeof(GradeDto))]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddGrade([FromBody] AddGradeRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid ModelState for AddGrade request: {@ModelStateErrors}", ModelState);
                return BadRequest(ModelState);
            }

            var currentUserId = GetCurrentUserId();

            try
            {
                _logger.LogInformation("Attempting to add grade for StudentId: {StudentId}, AssignmentId: {AssignmentId} by UserId: {CurrentUserId}", request.StudentId, request.TeacherSubjectGroupAssignmentId, currentUserId);
                var newGrade = await _gradeService.AddGradeAsync(request, currentUserId);
                _logger.LogInformation("Grade {GradeId} added successfully by UserId: {CurrentUserId}.", newGrade?.GradeId, currentUserId);
                return CreatedAtAction(nameof(GetGradeById), new { id = newGrade!.GradeId }, newGrade);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in AddGrade by UserId: {CurrentUserId}: {Message}", currentUserId, ex.Message);
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for AddGrade request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation occurred while adding grade: {Message}", ex.Message);
                return StatusCode(500, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while adding grade by UserId: {CurrentUserId}.", currentUserId);
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateGrade(int id, [FromBody] UpdateGradeRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid ModelState for UpdateGrade request (Grade ID: {GradeId}): {@ModelStateErrors}", id, ModelState);
                return BadRequest(ModelState);
            }

            var currentUserId = GetCurrentUserId();

            try
            {
                _logger.LogInformation("Attempting to update grade ID {GradeId} by UserId: {CurrentUserId}", id, currentUserId);
                var updated = await _gradeService.UpdateGradeAsync(id, request, currentUserId);
                if (!updated)
                {
                    _logger.LogWarning("Grade with ID {GradeId} not found or update failed by UserId: {CurrentUserId}.", id, currentUserId);
                    return NotFound($"Grade with ID {id} not found or no changes were made.");
                }

                _logger.LogInformation("Grade ID {GradeId} updated successfully by UserId: {CurrentUserId}.", id, currentUserId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in UpdateGrade by UserId: {CurrentUserId}: {Message}", currentUserId, ex.Message);
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for UpdateGrade request (Grade ID: {GradeId}): {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating grade ID {GradeId} by UserId: {CurrentUserId}.", id, currentUserId);
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var currentUserId = GetCurrentUserId();

            try
            {
                _logger.LogInformation("Attempting to delete grade ID {GradeId} by UserId: {CurrentUserId}", id, currentUserId);
                var deleted = await _gradeService.DeleteGradeAsync(id, currentUserId);
                if (!deleted)
                {
                    _logger.LogWarning("Grade with ID {GradeId} not found for deletion by UserId: {CurrentUserId}.", id, currentUserId);
                    return NotFound($"Grade with ID {id} not found.");
                }

                _logger.LogInformation("Grade ID {GradeId} deleted successfully by UserId: {CurrentUserId}.", id, currentUserId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt in DeleteGrade by UserId: {CurrentUserId}: {Message}", currentUserId, ex.Message);
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting grade ID {GradeId} by UserId: {CurrentUserId}.", id, currentUserId);
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }
    }
}
