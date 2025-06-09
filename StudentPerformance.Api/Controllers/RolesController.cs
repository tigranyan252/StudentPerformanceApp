// Path: StudentPerformance.Api/Controllers/RolesController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using StudentPerformance.Api.Services.Interfaces; // For IRoleService and IUserService
using StudentPerformance.Api.Exceptions; // For custom exceptions
using static StudentPerformance.Api.Utilities.UserRoles;
using StudentPerformance.Api.Services; // Для прямого доступа к константам ролей

namespace StudentPerformance.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All actions in this controller require authentication by default
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;

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
            throw new UnauthorizedAccessException("User ID claim not found or invalid in token.");
        }

        /// <summary>
        /// Gets a list of all roles.
        /// Requires Administrator role for full access.
        /// </summary>
        /// <returns>A list of Role DTOs.</returns>
        [HttpGet]
        [Authorize(Roles = Administrator)] // Только администратор может просматривать все роли
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Detailed authorization check
                bool authorized = await _userService.CanUserViewAllRolesAsync(currentUserId);
                if (!authorized)
                {
                    // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 403
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to view all roles." });
                }

                var roles = await _roleService.GetAllRolesAsync();
                return Ok(roles); // 200 OK
            }
            catch (UnauthorizedAccessException ex)
            {
                // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 401
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetRoles: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching roles.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific role by ID.
        /// Requires Administrator role, or the user's own role details.
        /// </summary>
        /// <param name="id">The ID of the role.</param>
        /// <returns>The Role DTO or NotFound if the role does not exist.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")] // Все аутентифицированные пользователи могут просматривать детали роли
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Detailed authorization check
                bool authorized = await _userService.CanUserViewRoleDetailsAsync(currentUserId, id);
                if (!authorized)
                {
                    // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 403
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to view this role's details." });
                }

                var role = await _roleService.GetRoleByIdAsync(id);

                if (role == null)
                {
                    return NotFound(new { message = $"Role with ID {id} not found." }); // 404 Not Found
                }

                return Ok(role); // 200 OK
            }
            catch (UnauthorizedAccessException ex)
            {
                // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 401
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetRole(id): {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching role details.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new role.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="createRoleDto">The data for the new role.</param>
        /// <returns>The created Role DTO or BadRequest if creation fails.</returns>
        [HttpPost]
        [Authorize(Roles = Administrator)] // Only Administrators can create roles
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // For ConflictException
        public async Task<ActionResult<RoleDto>> PostRole([FromBody] CreateRoleDto createRoleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();

                // Detailed authorization check
                bool authorized = await _userService.CanUserCreateRoleAsync(currentUserId);
                if (!authorized)
                {
                    // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 403
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to create roles." });
                }

                var createdRole = await _roleService.CreateRoleAsync(createRoleDto);

                // Service methods should throw exceptions for failures instead of returning null for cleaner handling
                return CreatedAtAction(nameof(GetRole), new { id = createdRole.RoleId }, createdRole); // 201 Created
            }
            catch (UnauthorizedAccessException ex)
            {
                // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 401
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = ex.Message });
            }
            catch (ConflictException ex) // Catch specific ConflictException
            {
                return Conflict(new { message = ex.Message }); // 409 Conflict
            }
            catch (ArgumentException ex) // For invalid arguments passed to service
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in PostRole: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while creating the role.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing role.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="id">The ID of the role to update.</param>
        /// <param name="updateRoleDto">The updated data for the role.</param>
        /// <returns>NoContent if successful, NotFound if the role doesn't exist, or BadRequest for invalid data.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = Administrator)] // Only Administrators can update roles
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // For ConflictException
        public async Task<IActionResult> PutRole(int id, [FromBody] UpdateRoleDto updateRoleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();

                // Detailed authorization check
                bool authorized = await _userService.CanUserUpdateRoleAsync(currentUserId, id);
                if (!authorized)
                {
                    // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 403
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to update roles." });
                }

                await _roleService.UpdateRoleAsync(id, updateRoleDto); // Service should throw exceptions on failure

                return NoContent(); // 204 No Content
            }
            catch (UnauthorizedAccessException ex)
            {
                // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 401
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = ex.Message });
            }
            catch (NotFoundException ex) // Catch specific NotFoundException
            {
                return NotFound(new { message = ex.Message }); // 404 Not Found
            }
            catch (ConflictException ex) // Catch specific ConflictException
            {
                return Conflict(new { message = ex.Message }); // 409 Conflict
            }
            catch (ArgumentException ex) // For invalid arguments passed to service
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in PutRole: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while updating the role.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a role.
        /// Requires Administrator role and fine-grained permission.
        /// </summary>
        /// <param name="id">The ID of the role to delete.</param>
        /// <returns>NoContent if successful, NotFound if the role does not exist.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = Administrator)] // Only Administrators can delete roles
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // For ConflictException (dependencies)
        public async Task<IActionResult> DeleteRole(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Detailed authorization check
                bool authorized = await _userService.CanUserDeleteRoleAsync(currentUserId, id);
                if (!authorized)
                {
                    // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 403
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to delete roles." });
                }

                await _roleService.DeleteRoleAsync(id); // Service should throw exceptions on failure

                return NoContent(); // 204 No Content
            }
            catch (UnauthorizedAccessException ex)
            {
                // ИСПРАВЛЕНИЕ: Используем StatusCode для возврата JSON тела с 401
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = ex.Message });
            }
            catch (NotFoundException ex) // Catch specific NotFoundException
            {
                return NotFound(new { message = ex.Message }); // 404 Not Found
            }
            catch (ConflictException ex) // Catch specific ConflictException (e.g., role has associated users)
            {
                return Conflict(new { message = ex.Message }); // 409 Conflict
            }
            catch (InvalidOperationException ex) // Catch for critical system roles check
            {
                return BadRequest(new { message = ex.Message }); // 400 Bad Request if it's a business rule violation
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in DeleteRole: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while deleting the role.", detail = ex.Message });
            }
        }
    }
}
