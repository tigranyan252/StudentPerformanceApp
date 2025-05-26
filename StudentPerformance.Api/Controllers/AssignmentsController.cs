using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services; // For IUserService and IAssignmentService
using StudentPerformance.Api.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // For ClaimsPrincipal and Claims
using System; // For UnauthorizedAccessException, InvalidOperationException
using Microsoft.AspNetCore.Http; // For StatusCodes

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Apply authorization globally to the controller
    public class AssignmentsController : ControllerBase
    {
        private readonly IUserService _userService; // Injected as interface
        private readonly IAssignmentService _assignmentService; // New dedicated service

        public AssignmentsController(IUserService userService, IAssignmentService assignmentService)
        {
            _userService = userService;
            _assignmentService = assignmentService;
        }

        // Helper to get the user ID from claims
        // Consider this helper in a base controller or extension method if used frequently.
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            // If the user ID is not found, it means the token is invalid or corrupted.
            // This situation ideally should be caught by authentication middleware first.
            // Throwing InvalidOperationException is appropriate for an unexpected state.
            throw new InvalidOperationException("User ID claim not found or invalid in token.");
        }

        /// <summary>
        /// Adds a new teacher-subject-group assignment.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="request">The assignment data.</param>
        /// <returns>A DTO of the newly created assignment or BadRequest if invalid.</returns>
        [HttpPost]
        [Authorize(Roles = "Администратор")] // Explicitly define role for clarity and direct authorization
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // 403 will be returned by [Authorize] if role doesn't match
        public async Task<IActionResult> AddAssignment([FromBody] AddTeacherSubjectGroupAssignmentRequest request)
        {
            // We can remove the _userService.CanUserAddAssignmentAsync check here
            // if [Authorize(Roles = "Administrator")] is sufficient, as it handles the 403.
            // If you have more complex logic than just role, keep a service check.
            // For this example, let's rely on [Authorize] attribute.

            var assignmentDto = await _assignmentService.AddAssignmentAsync(request);

            if (assignmentDto == null)
            {
                // This could mean a duplicate assignment or invalid foreign keys (teacher, subject, group, semester not found).
                // Returning BadRequest with a specific message is good.
                return BadRequest("Failed to add assignment. It might already exist or related entities are invalid.");
            }

            return CreatedAtAction(
                nameof(GetAssignmentById), // Name of the GET method to retrieve the created resource
                new { assignmentId = assignmentDto.TeacherSubjectGroupAssignmentId }, // Route values for the GET method
                assignmentDto // The created resource itself
            );
        }

        /// <summary>
        /// Gets a specific assignment by its ID.
        /// Requires Administrator or Teacher (if assigned) roles.
        /// </summary>
        /// <param name="assignmentId">The ID of the assignment.</param>
        /// <returns>The assignment DTO or NotFound.</returns>
        [HttpGet("{assignmentId}")]
        // More complex authorization logic needed here, potentially combining roles and ownership
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAssignmentById(int assignmentId)
        {
            var currentUserId = GetCurrentUserId(); // Get user ID from token

            // This check needs to be more complex if a Teacher can only see their own assignments
            // It might look like: await _userService.IsUserAdmin(currentUserId) || await _userService.IsUserAssignedToAssignment(currentUserId, assignmentId)
            // Assuming CanUserViewAssignmentDetailsAsync covers this complex logic.
            if (!await _userService.CanUserViewAssignmentDetailsAsync(currentUserId, assignmentId))
            {
                return Forbid(); // 403 Forbidden - User lacks permission
            }

            var assignmentDto = await _assignmentService.GetAssignmentByIdAsync(assignmentId);

            if (assignmentDto == null)
            {
                return NotFound(); // 404 Not Found - Assignment does not exist
            }

            return Ok(assignmentDto); // 200 OK
        }

        /// <summary>
        /// Gets all teacher-subject-group assignments.
        /// Requires Administrator or Teacher roles.
        /// </summary>
        /// <returns>A list of assignment DTOs.</returns>
        [HttpGet]
        [Authorize(Roles = "Администратор,Преподаватель")] // Users with either role can view all
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllAssignments()
        {
            // You might add a check here if Teachers should only see *their own* assignments
            // If so, you'd filter the list based on currentUserId.
            // For now, assuming "view all" means all if they have the role.
            // If an Admin sees all and a Teacher sees only theirs, this needs branching logic.

            var assignments = await _assignmentService.GetAllAssignmentsAsync();
            return Ok(assignments);
        }

        /// <summary>
        /// Updates an existing teacher-subject-group assignment.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="assignmentId">The ID of the assignment to update.</param>
        /// <param name="request">The updated data for the assignment.</param>
        /// <returns>NoContent if successful, NotFound or BadRequest otherwise.</returns>
        [HttpPut("{assignmentId}")]
        [Authorize(Roles = "Администратор")] // Only administrators can update
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateAssignment(int assignmentId, [FromBody] UpdateTeacherSubjectGroupAssignmentRequest request)
        {
            // Similar to Add, rely on [Authorize] for simple role checks.
            // If complex permission check needed, keep _userService.CanUserUpdateAssignmentAsync
            // var currentUserId = GetCurrentUserId();
            // if (!await _userService.CanUserUpdateAssignmentAsync(currentUserId, assignmentId)) { return Forbid(); }

            var isUpdated = await _assignmentService.UpdateAssignmentAsync(assignmentId, request);

            if (isUpdated)
            {
                return NoContent(); // 204 No Content for successful update
            }

            // If not updated, it could be not found or a business logic error (e.g., duplicate).
            // A more specific error message or returning a detailed object could be better.
            return NotFound("Assignment not found or update failed due to data conflict (e.g., duplicate assignment after changes).");
        }

        /// <summary>
        /// Deletes a teacher-subject-group assignment.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="assignmentId">The ID of the assignment to delete.</param>
        /// <returns>NoContent if successful, NotFound otherwise.</returns>
        [HttpDelete("{assignmentId}")]
        [Authorize(Roles = "Администратор")] // Only administrators can delete
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteAssignment(int assignmentId)
        {
            // Similar to Add/Update, rely on [Authorize] for simple role checks.
            // var currentUserId = GetCurrentUserId();
            // if (!await _userService.CanUserDeleteAssignmentAsync(currentUserId, assignmentId)) { return Forbid(); }

            var isDeleted = await _assignmentService.DeleteAssignmentAsync(assignmentId);

            if (isDeleted)
            {
                return NoContent(); // 204 No Content for successful deletion
            }

            return NotFound(); // 404 Not Found if assignment doesn't exist or could not be deleted
        }
    }
}