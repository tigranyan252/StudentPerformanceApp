// Path: StudentPerformance.Api/Controllers/StudentsController.cs

using Microsoft.AspNetCore.Mvc;
// Change this: using StudentPerformance.Api.Services;
using StudentPerformance.Api.Services; // Assuming both IStudentService and UserService are in this namespace

using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AutoMapper;

using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using StudentPerformance.Api.Models.QueryParameters;

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        // Change this: private readonly UserService _userService;
        private readonly IStudentService _studentService; // Inject the interface
        private readonly IMapper _mapper;

        // Change this: public StudentsController(UserService userService, IMapper mapper)
        public StudentsController(IStudentService studentService, IMapper mapper)
        {
            _studentService = studentService; // Assign the injected service
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = "Администратор,Преподаватель")]
        public async Task<ActionResult<List<StudentDto>>> GetAllStudents()
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            // Use _studentService for student-specific authorization and data fetching
            bool authorized = await _studentService.CanUserViewAllStudentsAsync(currentUserId);

            if (!authorized)
            {
                return Forbid();
            }

            var students = await _studentService.GetAllStudentsAsync(); // Call the student service
            return Ok(students);
        }

        [HttpGet("{studentId}")]
        [Authorize(Roles = "Администратор,Преподаватель,Студент")]
        public async Task<ActionResult<StudentDto>> GetStudentById(int studentId)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _studentService.CanUserViewStudentDetailsAsync(currentUserId, studentId);

            if (!authorized)
            {
                return Forbid();
            }

            var student = await _studentService.GetStudentByIdAsync(studentId);

            if (student == null)
            {
                return Forbid();
            }

            return Ok(student);
        }

        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AddStudent([FromBody] AddStudentRequest request)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _studentService.CanUserAddStudentAsync(currentUserId);

            if (!authorized)
            {
                return Forbid();
            }

            var addedStudentDto = await _studentService.AddStudentAsync(request);

            if (addedStudentDto == null)
            {
                return BadRequest("Failed to add student. Ensure the specified GroupId exists.");
            }

            return CreatedAtAction(nameof(GetStudentById), new { studentId = addedStudentDto.StudentId }, addedStudentDto);
        }

        [HttpPut("{studentId}")]
        [Authorize(Roles = "Администратор,Студент")]
        public async Task<IActionResult> UpdateStudent(int studentId, [FromBody] UpdateStudentRequest request)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _studentService.CanUserUpdateStudentAsync(currentUserId, studentId);

            if (!authorized)
            {
                return Forbid();
            }

            var isUpdated = await _studentService.UpdateStudentAsync(studentId, request);

            if (!isUpdated)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{studentId}")]
        [Authorize(Roles = "Администратор,Студент")]
        public async Task<IActionResult> DeleteStudent(int studentId)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _studentService.CanUserDeleteStudentAsync(currentUserId, studentId);

            if (!authorized)
            {
                return Forbid();
            }

            var isDeleted = await _studentService.DeleteStudentAsync(studentId);

            if (!isDeleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}