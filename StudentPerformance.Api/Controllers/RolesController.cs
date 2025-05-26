// Path: Controllers/RolesController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs; // <--- Corrected DTO namespace
using StudentPerformance.Api.Services; // For IRoleService and IUserService
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // For ClaimTypes
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For StatusCodes
using System;
using StudentPerformance.Api.Services.Interfaces; // For InvalidOperationException

namespace StudentPerformance.Api.Controllers
{
    [Route("api/[controller]")] // Sets the base route, e.g., /api/roles
    [ApiController] // Indicates this is an API controller
    [Authorize] // All actions in this controller require authentication by default
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly IUserService _userService; // Inject IUserService for shared auth logic

        public RolesController(IRoleService roleService, IUserService userService)
        {
            _roleService = roleService;
            _userService = userService;
        }

        // Helper to get the user ID from claims
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
        /// Gets a list of all roles.
        /// Requires Administrator, Teacher, or Student roles, and fine-grained permission.
        /// </summary>
        /// <returns>A list of Role DTOs.</returns>
        [HttpGet]
        [Authorize(Roles = "Администратор,Преподаватель,Студент")] // Example: All authenticated users can view roles
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var currentUserId = GetCurrentUserId();

            // Detailed authorization check (e.g., should user be able to see all roles?)
            bool authorized = await _userService.CanUserViewAllRolesAsync(currentUserId);
            if (!authorized)
            {
                return Forbid(); // HTTP 403 Forbidden
            }

            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles); // 200 OK
        }

        /// <summary>
        /// Gets a specific role by ID.
        /// Requires Administrator, Teacher, or Student roles, and fine-grained permission.
        /// </summary>
        /// <param name="id">The ID of the role.</param>
        /// <returns>The Role DTO or NotFound if the role does not exist.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Администратор,Преподаватель,Студент")] // Example: All authenticated users can view specific role details
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            var currentUserId = GetCurrentUserId();

            // Detailed authorization check
            bool authorized = await _userService.CanUserViewRoleDetailsAsync(currentUserId, id);
            if (!authorized)
            {
                return Forbid();
            }

            var role = await _roleService.GetRoleByIdAsync(id);

            if (role == null)
            {
                return NotFound(); // 404 Not Found if the role ID doesn't exist
            }

            return Ok(role); // 200 OK
        }

        /// <summary>
        /// Creates a new role.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="createRoleDto">The data for the new role.</param>
        /// <returns>The created Role DTO or BadRequest if creation fails.</returns>
        [HttpPost]
        [Authorize(Roles = "Администратор")] // Only Administrators can create roles
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // For validation errors or business logic (e.g., duplicate name)
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<RoleDto>> PostRole([FromBody] CreateRoleDto createRoleDto)
        {
            var currentUserId = GetCurrentUserId();

            // Detailed authorization check
            bool authorized = await _userService.CanUserCreateRoleAsync(currentUserId);
            if (!authorized)
            {
                return Forbid();
            }

            var createdRole = await _roleService.CreateRoleAsync(createRoleDto);

            if (createdRole == null)
            {
                // This might indicate a business rule violation like "role name already exists"
                // or other validation issues, if the service returns null for such cases.
                return BadRequest("Failed to create role. Check request data (e.g., role name might not be unique).");
            }

            // Returns 201 Created status code and a Location header with the URI of the newly created resource.
            return CreatedAtAction(nameof(GetRole), new { id = createdRole.RoleId }, createdRole);
        }

        /// <summary>
        /// Updates an existing role.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="id">The ID of the role to update.</param>
        /// <param name="updateRoleDto">The updated data for the role.</param>
        /// <returns>NoContent if successful, NotFound if the role doesn't exist, or BadRequest for invalid data.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Администратор")] // Only Administrators can update roles
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PutRole(int id, [FromBody] UpdateRoleDto updateRoleDto)
        {
            var currentUserId = GetCurrentUserId();

            // Detailed authorization check
            bool authorized = await _userService.CanUserUpdateRoleAsync(currentUserId, id);
            if (!authorized)
            {
                return Forbid();
            }

            // Basic validation for ID:
            if (id <= 0)
            {
                return BadRequest("Invalid Role ID."); // 400 Bad Request
            }

            var updatedRoleDto = await _roleService.UpdateRoleAsync(id, updateRoleDto); // Service returns RoleDto?

            if (updatedRoleDto == null) // Check if 'updatedRoleDto' is null
            {
                // If update failed, it's likely because the role with 'id' was not found.
                // Or due to other business logic (e.g., duplicate name).
                // A more robust service might return an enum or specific error type for different failures.
                return NotFound(); // 404 Not Found
            }

            return NoContent(); // 204 No Content (standard for successful PUT with no body)
        }

        /// <summary>
        /// Deletes a role.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="id">The ID of the role to delete.</param>
        /// <returns>NoContent if successful, NotFound if the role does not exist.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Администратор")] // Only Administrators can delete roles
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var currentUserId = GetCurrentUserId();

            // Detailed authorization check
            bool authorized = await _userService.CanUserDeleteRoleAsync(currentUserId, id);
            if (!authorized)
            {
                return Forbid();
            }

            var deleted = await _roleService.DeleteRoleAsync(id); // Service returns bool

            if (!deleted)
            {
                // If deletion failed, likely because the role was not found.
                // Or due to other business logic (e.g., cannot delete a role that has assigned users).
                // A more robust service might return an enum or specific error type for different failures.
                return NotFound(); // 404 Not Found
            }

            return NoContent(); // 204 No Content
        }
    }
}