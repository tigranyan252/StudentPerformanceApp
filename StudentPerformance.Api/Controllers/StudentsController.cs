// Path: StudentPerformance.Api/Controllers/StudentsController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AutoMapper;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces;
using System.Collections.Generic;
using System;
using static StudentPerformance.Api.Utilities.UserRoles;
using StudentPerformance.Api.Exceptions; // ДОБАВЛЕНО: Для кастомных исключений
using Microsoft.Extensions.Logging;
using StudentPerformance.Api.Services;
// using StudentPerformance.Api.Services; // Эту строку можно удалить, так как напрямую не ссылаемся на конкретные реализации сервисов
// Если здесь нет других прямых использований StudentPerformance.Api.Services,
// то эта директива не нужна. ILogger уже инжектируется.


namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(IStudentService studentService, IMapper mapper, IUserService userService, ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _mapper = mapper;
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

            _logger.LogWarning("GetCurrentUserId: User ID claim not found or is invalid.");
            throw new UnauthorizedAccessException("User ID claim not found or is invalid.");
        }

        [HttpGet]
        [Authorize(Roles = $"{Administrator},{Teacher}")]
        public async Task<ActionResult<List<StudentDto>>> GetAllStudents(
            [FromQuery] int? groupId,
            [FromQuery] string? userName)
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserViewAllStudentsAsync(currentUserId);

                if (!authorized)
                {
                    _logger.LogWarning("GetAllStudents: User {UserId} is not authorized to view all students.", currentUserId);
                    return StatusCode(403, new { message = "You are not authorized to view all students." });
                }

                var students = await _studentService.GetAllStudentsAsync(groupId, userName);
                return Ok(students);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to GetAllStudents.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all students.");
                return StatusCode(500, new { message = "An error occurred while retrieving students." });
            }
        }

        [HttpGet("{studentId}")]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")]
        public async Task<ActionResult<StudentDto>> GetStudentById(int studentId)
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserViewStudentDetailsAsync(currentUserId, studentId);

                if (!authorized)
                {
                    _logger.LogWarning("GetStudentById: User {UserId} is not authorized to view student {StudentId}.", currentUserId, studentId);
                    return StatusCode(403, new { message = "You are not authorized to view this student's details." });
                }

                var student = await _studentService.GetStudentByIdAsync(studentId);

                if (student == null)
                {
                    return NotFound($"Student with ID {studentId} not found.");
                }

                return Ok(student);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to GetStudentById for student {StudentId}.", studentId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching student {StudentId} by ID.", studentId);
                return StatusCode(500, new { message = $"An error occurred while retrieving student with ID {studentId}." });
            }
        }

        // НОВЫЙ МЕТОД: Получение студента по User ID
        [HttpGet("user/{userId}")] // Маршрут: /api/students/user/{userId}
        [ProducesResponseType(200, Type = typeof(StudentDto))]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")] // Доступно для всех трех ролей
        public async Task<IActionResult> GetStudentByUserId(int userId)
        {
            try
            {
                var currentAuthUserId = GetCurrentUserId();
                var currentUserRole = await _userService.GetUserRoleAsync(currentAuthUserId);

                _logger.LogInformation("Attempting to get student profile by UserId: {UserId} for authenticated UserId: {CurrentAuthUserId}, Role: {CurrentUserRole}", (object)userId, (object)currentAuthUserId, (object)currentUserRole);

                if (currentUserRole == Student)
                {
                    // Студент может получить только свой собственный профиль
                    if (userId != currentAuthUserId)
                    {
                        _logger.LogWarning("Student {CurrentAuthUserId} attempted to access profile for UserId {RequestedUserId}. Access denied.", (object)currentAuthUserId, (object)userId);
                        return Forbid("You are only authorized to view your own student profile.");
                    }
                }
                else if (currentUserRole == Teacher)
                {
                    // Преподаватель может получить профиль студента, если студент находится в группе,
                    // которая назначена этому преподавателю.
                    var teacherProfile = await _userService.GetTeacherByIdAsync(currentAuthUserId);
                    if (teacherProfile == null)
                    {
                        _logger.LogWarning("Teacher profile not found for UserId: {CurrentAuthUserId}. Access denied.", (object)currentAuthUserId);
                        return Forbid("Teacher profile not found.");
                    }

                    var studentProfile = await _studentService.GetStudentByUserIdAsync(userId); // Получаем студента
                    if (studentProfile == null)
                    {
                        _logger.LogWarning("Student profile for UserId {UserId} not found.", (object)userId);
                        return NotFound($"Student profile for User ID {userId} not found.");
                    }

                    // Проверяем, преподает ли этот учитель студенту через TeacherSubjectGroupAssignment
                    var isStudentInTeacherAssignedGroup = await _studentService.IsStudentInTeacherAssignedGroupAsync(teacherProfile.TeacherId, studentProfile.StudentId);
                    if (!isStudentInTeacherAssignedGroup)
                    {
                        _logger.LogWarning("Teacher {TeacherId} attempted to access student profile {StudentId} not in their assigned groups. Access denied.", (object)teacherProfile.TeacherId, (object)studentProfile.StudentId);
                        return Forbid("You are not authorized to view this student's profile.");
                    }
                }
                // Администратор имеет полный доступ, ему не нужны дополнительные проверки здесь

                var student = await _studentService.GetStudentByUserIdAsync(userId);
                if (student == null)
                {
                    _logger.LogWarning("Student profile for User ID {UserId} not found.", (object)userId);
                    return NotFound($"Student profile for User ID {userId} not found.");
                }

                _logger.LogInformation("Student profile for User ID {UserId} retrieved successfully by UserId: {CurrentAuthUserId}.", (object)userId, (object)currentAuthUserId);
                return Ok(student);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to student profile by UserId: {CurrentAuthUserId}: {Message}", (object)GetCurrentUserId(), ex.Message);
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching student profile for User ID {UserId} by UserId: {CurrentAuthUserId}.", (object)userId, (object)GetCurrentUserId());
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }


        [HttpPost]
        [Authorize(Roles = Administrator)]
        public async Task<IActionResult> AddStudent([FromBody] AddStudentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserAddStudentAsync(currentUserId);

                if (!authorized)
                {
                    _logger.LogWarning("AddStudent: User {UserId} is not authorized to add students.", currentUserId);
                    return StatusCode(403, new { message = "You are not authorized to add students." });
                }

                var addedStudentDto = await _studentService.AddStudentAsync(request);

                if (addedStudentDto == null)
                {
                    _logger.LogError("AddStudent: Student service returned null after adding student, but no exception was thrown. This indicates an unexpected scenario.");
                    return BadRequest("Failed to add student due to an unexpected issue.");
                }

                return CreatedAtAction(nameof(GetStudentById), new { studentId = addedStudentDto.StudentId }, addedStudentDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "AddStudent: Bad request due to argument error: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "AddStudent: Server configuration error: {Message}", ex.Message);
                return StatusCode(500, new { message = "Server configuration error: " + ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to AddStudent.");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a student.");
                return StatusCode(500, new { message = "An error occurred while adding the student." });
            }
        }

        [HttpPut("{studentId}")]
        [Authorize(Roles = $"{Administrator},{Student}")]
        public async Task<IActionResult> UpdateStudent(int studentId, [FromBody] UpdateStudentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserUpdateStudentAsync(currentUserId, studentId);

                if (!authorized)
                {
                    _logger.LogWarning("UpdateStudent: User {UserId} is not authorized to update student {StudentId}.", currentUserId, studentId);
                    return StatusCode(403, new { message = "You are not authorized to update this student." });
                }

                var isUpdated = await _studentService.UpdateStudentAsync(studentId, request);

                if (!isUpdated)
                {
                    return NotFound($"Student with ID {studentId} not found or no changes were made.");
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to UpdateStudent for student {StudentId}.", studentId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating student with ID: {StudentId}", studentId);
                return StatusCode(500, new { message = $"An error occurred while updating student with ID {studentId}." });
            }
        }

        [HttpDelete("{studentId}")]
        [Authorize(Roles = Administrator)]
        public async Task<IActionResult> DeleteStudent(int studentId)
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                bool authorized = await _userService.CanUserDeleteStudentAsync(currentUserId, studentId);

                if (!authorized)
                {
                    _logger.LogWarning("DeleteStudent: User {UserId} is not authorized to delete student {StudentId}.", currentUserId, studentId);
                    return StatusCode(403, new { message = "You are not authorized to delete this student." });
                }

                await _studentService.DeleteStudentAsync(studentId);

                _logger.LogInformation("DeleteStudent: Student {StudentId} deleted successfully via controller.", studentId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "DeleteStudent: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "DeleteStudent: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt to DeleteStudent for student {StudentId}.", studentId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting student with ID: {StudentId}", studentId);
                return StatusCode(500, new { message = $"An unexpected error occurred while deleting student with ID {studentId}." });
            }
        }
    }
}
