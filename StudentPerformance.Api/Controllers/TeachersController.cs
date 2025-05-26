// Path: Controllers/TeachersController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services; // Now for ITeacherService
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeachersController : ControllerBase
    {
        // Inject ITeacherService
        private readonly ITeacherService _teacherService;
        // You might still need IUserService for *general* user operations if any.
        // private readonly IUserService _userService; 

        public TeachersController(ITeacherService teacherService /*, IUserService userService if needed */)
        {
            _teacherService = teacherService;
            // _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Администратор,Преподаватель")]
        public async Task<ActionResult<IEnumerable<TeacherDto>>> GetAllTeachers()
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            // Use _teacherService for teacher-specific authorization and data fetching
            bool authorized = await _teacherService.CanUserViewAllTeachersAsync(currentUserId);
            if (!authorized)
            {
                return Forbid();
            }

            var teachers = await _teacherService.GetAllTeachersAsync();
            return Ok(teachers);
        }

        [HttpGet("{teacherId}")]
        [Authorize(Roles = "Администратор,Преподаватель")]
        public async Task<ActionResult<TeacherDto>> GetTeacherById(int teacherId)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _teacherService.CanUserViewTeacherDetailsAsync(currentUserId, teacherId);
            if (!authorized)
            {
                return Forbid();
            }

            var teacher = await _teacherService.GetTeacherByIdAsync(teacherId);
            // After authorization, if the teacher is genuinely not found by the service, return NotFound.
            if (teacher == null)
            {
                return NotFound();
            }

            return Ok(teacher);
        }

        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AddTeacher([FromBody] AddTeacherRequest request)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _teacherService.CanUserAddTeacherAsync(currentUserId);
            if (!authorized)
            {
                return Forbid();
            }

            var addedTeacherDto = await _teacherService.AddTeacherAsync(request);

            if (addedTeacherDto == null)
            {
                // This might indicate a business rule violation (e.g., login already exists)
                return BadRequest("Failed to add teacher. The login might already be in use or the request data is invalid.");
            }

            return CreatedAtAction(nameof(GetTeacherById), new { teacherId = addedTeacherDto.TeacherId }, addedTeacherDto);
        }

        [HttpPut("{teacherId}")]
        [Authorize(Roles = "Администратор,Преподаватель")]
        public async Task<IActionResult> UpdateTeacher(int teacherId, [FromBody] UpdateTeacherRequest request)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _teacherService.CanUserUpdateTeacherAsync(currentUserId, teacherId);
            if (!authorized)
            {
                return Forbid();
            }

            var isUpdated = await _teacherService.UpdateTeacherAsync(teacherId, request);

            if (!isUpdated)
            {
                // If update failed, likely because the teacher was not found or some business rule was violated.
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{teacherId}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> DeleteTeacher(int teacherId)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _teacherService.CanUserDeleteTeacherAsync(currentUserId, teacherId);
            if (!authorized)
            {
                return Forbid();
            }

            var isDeleted = await _teacherService.DeleteTeacherAsync(teacherId);

            if (!isDeleted)
            {
                // If deletion failed, likely because the teacher was not found.
                return NotFound();
            }

            return NoContent();
        }
    }
}