// Path: Controllers/SemestersController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services; // Now for ISemesterService and IUserService
using StudentPerformance.Api.Models.DTOs; // For SemesterDto, AddSemesterRequest, UpdateSemesterRequest
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Crucial for current user's ID
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For StatusCodes
using System; // For InvalidOperationException

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All actions in this controller require authentication by default
    public class SemestersController : ControllerBase
    {
        // Inject dedicated services
        private readonly ISemesterService _semesterService;
        private readonly IUserService _userService; // Still needed for authorization checks

        // Controller Constructor
        public SemestersController(ISemesterService semesterService, IUserService userService)
        {
            _semesterService = semesterService;
            _userService = userService;
        }

        // Helper to get the current user ID from claims
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            throw new InvalidOperationException("User ID claim not found or invalid in token.");
        }

        /// <summary>
        /// Gets a list of all semesters.
        /// Requires Administrator, Teacher, or Student roles, and fine-grained permission.
        /// </summary>
        /// <returns>A list of Semester DTOs.</returns>
        [HttpGet]
        [Authorize(Roles = "Администратор,Преподаватель,Студент")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<SemesterDto>>> GetAllSemesters()
        {
            var currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserViewAllSemestersAsync(currentUserId);
            if (!authorized)
            {
                return Forbid();
            }

            var semesters = await _semesterService.GetAllSemestersAsync(); // Call dedicated semester service
            return Ok(semesters);
        }

        /// <summary>
        /// Gets a specific semester by its ID.
        /// Requires Administrator, Teacher, or Student roles, and fine-grained permission.
        /// </summary>
        /// <param name="semesterId">The ID of the semester.</param>
        /// <returns>The Semester DTO or NotFound if the semester does not exist.</returns>
        [HttpGet("{semesterId}")]
        [Authorize(Roles = "Администратор,Преподаватель,Студент")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Added 404
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SemesterDto>> GetSemesterById(int semesterId)
        {
            var currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserViewSemesterDetailsAsync(currentUserId, semesterId);
            if (!authorized)
            {
                // If authorization denies access, it could be because the semester doesn't exist
                // or the user simply isn't allowed to see it. For security, Forbid is appropriate.
                return Forbid();
            }

            var semester = await _semesterService.GetSemesterByIdAsync(semesterId); // Call dedicated semester service

            if (semester == null)
            {
                // This case should ideally be handled by CanUserViewSemesterDetailsAsync,
                // but as a fallback, explicitly return NotFound if the service indicates absence.
                return NotFound();
            }

            return Ok(semester);
        }

        /// <summary>
        /// Adds a new semester.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="request">The data for the new semester.</param>
        /// <returns>A DTO of the newly created semester or BadRequest if invalid.</returns>
        [HttpPost]
        [Authorize(Roles = "Администратор")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddSemester([FromBody] AddSemesterRequest request)
        {
            var currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserAddSemesterAsync(currentUserId);
            if (!authorized)
            {
                return Forbid();
            }

            var addedSemesterDto = await _semesterService.AddSemesterAsync(request); // Call dedicated semester service

            if (addedSemesterDto == null)
            {
                return BadRequest("Failed to add semester. It might already exist or invalid data provided (e.g., date overlap).");
            }

            return CreatedAtAction(nameof(GetSemesterById), new { semesterId = addedSemesterDto.SemesterId }, addedSemesterDto);
        }

        /// <summary>
        /// Updates an existing semester.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="semesterId">The ID of the semester to update.</param>
        /// <param name="request">The updated data for the semester.</param>
        /// <returns>NoContent if successful, NotFound if the semester doesn't exist, or BadRequest for invalid data.</returns>
        [HttpPut("{semesterId}")]
        [Authorize(Roles = "Администратор")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateSemester(int semesterId, [FromBody] UpdateSemesterRequest request)
        {
            var currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserUpdateSemesterAsync(currentUserId, semesterId);
            if (!authorized)
            {
                return Forbid();
            }

            var isUpdated = await _semesterService.UpdateSemesterAsync(semesterId, request); // Call dedicated semester service

            if (!isUpdated)
            {
                // If update failed, check if it was because the semester wasn't found
                var existingSemester = await _semesterService.GetSemesterByIdAsync(semesterId);
                if (existingSemester == null)
                {
                    return NotFound($"Semester with ID {semesterId} not found.");
                }
                // Otherwise, it's a business logic error (e.g., date overlap, duplicate name)
                return BadRequest("Failed to update semester. Check for data conflicts.");
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes a semester.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="semesterId">The ID of the semester to delete.</param>
        /// <returns>NoContent if successful, NotFound if the semester does not exist.</returns>
        [HttpDelete("{semesterId}")]
        [Authorize(Roles = "Администратор")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteSemester(int semesterId)
        {
            var currentUserId = GetCurrentUserId();

            bool authorized = await _userService.CanUserDeleteSemesterAsync(currentUserId, semesterId);
            if (!authorized)
            {
                return Forbid();
            }

            var isDeleted = await _semesterService.DeleteSemesterAsync(semesterId); // Call dedicated semester service

            if (!isDeleted)
            {
                return NotFound($"Semester with ID {semesterId} not found.");
            }

            return NoContent();
        }
    }
}