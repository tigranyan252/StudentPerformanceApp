// Path: Controllers/SubjectsController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services; // Now for ISubjectService, IUserService (if still needed for general user checks)
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
    public class SubjectsController : ControllerBase
    {
        // Inject ISubjectService
        private readonly ISubjectService _subjectService;
        // If you still need IUserService for other *general* user operations (not subject-specific authorization), keep it.
        // private readonly IUserService _userService;

        public SubjectsController(ISubjectService subjectService /*, IUserService userService if needed */)
        {
            _subjectService = subjectService;
            // _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Администратор,Преподаватель,Студент")]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAllSubjects()
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            // Use _subjectService for subject-specific authorization and data fetching
            bool authorized = await _subjectService.CanUserViewAllSubjectsAsync(currentUserId);
            if (!authorized)
            {
                return Forbid();
            }

            var subjects = await _subjectService.GetAllSubjectsAsync();
            return Ok(subjects);
        }

        [HttpGet("{subjectId}")]
        [Authorize(Roles = "Администратор,Преподаватель,Студент")]
        public async Task<ActionResult<SubjectDto>> GetSubjectById(int subjectId)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _subjectService.CanUserViewSubjectDetailsAsync(currentUserId, subjectId);
            if (!authorized)
            {
                return Forbid();
            }

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            // After CanUserViewSubjectDetailsAsync confirms existence and authorization,
            // if GetSubjectByIdAsync still returns null, it's an internal consistency issue.
            // For general API behavior, returning NotFound if the subject is genuinely not found is more appropriate.
            if (subject == null)
            {
                return NotFound(); // Or Forbid() if you want to be stricter about existence via auth
            }

            return Ok(subject);
        }

        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AddSubject([FromBody] AddSubjectRequest request)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _subjectService.CanUserAddSubjectAsync(currentUserId);
            if (!authorized)
            {
                return Forbid();
            }

            var addedSubjectDto = await _subjectService.AddSubjectAsync(request);

            if (addedSubjectDto == null)
            {
                // This could mean a duplicate name or other business rule violation
                return BadRequest("Failed to add subject. The subject might already exist or the request data is invalid.");
            }

            return CreatedAtAction(nameof(GetSubjectById), new { subjectId = addedSubjectDto.SubjectId }, addedSubjectDto);
        }

        [HttpPut("{subjectId}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> UpdateSubject(int subjectId, [FromBody] UpdateSubjectRequest request)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _subjectService.CanUserUpdateSubjectAsync(currentUserId, subjectId);
            if (!authorized)
            {
                return Forbid();
            }

            var isUpdated = await _subjectService.UpdateSubjectAsync(subjectId, request);

            if (!isUpdated)
            {
                // This typically means the subject was not found or a business rule prevented the update (e.g., duplicate name).
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{subjectId}")]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> DeleteSubject(int subjectId)
        {
            string? currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();
            int currentUserId = int.Parse(currentUserIdString);

            bool authorized = await _subjectService.CanUserDeleteSubjectAsync(currentUserId, subjectId);
            if (!authorized)
            {
                return Forbid();
            }

            var isDeleted = await _subjectService.DeleteSubjectAsync(subjectId);

            if (!isDeleted)
            {
                // This typically means the subject was not found.
                return NotFound();
            }

            return NoContent();
        }
    }
}