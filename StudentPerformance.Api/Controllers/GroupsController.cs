// Path: Controllers/GroupsController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services; // For IGroupService
using StudentPerformance.Api.Models.DTOs; // For GroupDto, AddGroupRequest, UpdateGroupRequest
using Microsoft.AspNetCore.Authorization; // For [Authorize]
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For StatusCodes

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/groups")]
    [Authorize] // All actions in this controller require authentication by default
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService; // Injected as interface

        public GroupsController(IGroupService groupService) // Constructor injects IGroupService
        {
            _groupService = groupService;
        }

        /// <summary>
        /// Gets a list of all groups.
        /// </summary>
        /// <returns>A list of Group DTOs.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Implicit from [Authorize]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // If role-based authorization was present here
        public async Task<ActionResult<List<GroupDto>>> GetAllGroups()
        {
            var groups = await _groupService.GetAllGroupsAsync();
            return Ok(groups);
        }

        /// <summary>
        /// Gets a specific group by its ID.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <returns>The Group DTO or NotFound if the group does not exist.</returns>
        [HttpGet("{groupId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // If role-based or complex auth was present
        public async Task<ActionResult<GroupDto>> GetGroupById(int groupId)
        {
            var group = await _groupService.GetGroupByIdAsync(groupId);

            if (group == null)
            {
                return NotFound();
            }

            return Ok(group);
        }

        /// <summary>
        /// Adds a new group.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="request">The data for the new group.</param>
        /// <returns>A DTO of the newly created group or BadRequest if invalid.</returns>
        [HttpPost]
        [Authorize(Roles = "Администратор")] // Only Administrator can add groups
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // For validation errors or business logic errors (e.g., duplicate name)
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddGroup([FromBody] AddGroupRequest request)
        {
            // ASP.NET Core's [ApiController] handles automatic model validation.
            // If the request model is invalid, a 400 BadRequest is returned automatically.

            var addedGroupDto = await _groupService.AddGroupAsync(request);

            if (addedGroupDto == null)
            {
                // This could indicate a business rule violation, e.g., group with this name already exists.
                return BadRequest("Failed to add group. It might already exist or invalid data provided.");
            }

            return CreatedAtAction(nameof(GetGroupById), new { groupId = addedGroupDto.GroupId }, addedGroupDto);
        }

        /// <summary>
        /// Updates an existing group.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="groupId">The ID of the group to update.</param>
        /// <param name="request">The updated data for the group.</param>
        /// <returns>NoContent if successful, NotFound if the group doesn't exist, or BadRequest for invalid data.</returns>
        [HttpPut("{groupId}")]
        [Authorize(Roles = "Администратор")] // Only Administrator can update groups
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] UpdateGroupRequest request)
        {
            var isUpdated = await _groupService.UpdateGroupAsync(groupId, request);

            if (!isUpdated)
            {
                // If update failed, check if it was because the group wasn't found
                // If your service returns false for other business rule violations (e.g., duplicate name),
                // you might need more specific error handling/return types from the service.
                var existingGroup = await _groupService.GetGroupByIdAsync(groupId);
                if (existingGroup == null)
                {
                    return NotFound($"Group with ID {groupId} not found.");
                }
                // If group exists but update failed for another reason (e.g., duplicate name after update),
                // it's more appropriate to return BadRequest.
                return BadRequest("Failed to update group. Check for duplicate names or other data conflicts.");
            }

            return NoContent(); // 204 No Content for successful update
        }

        /// <summary>
        /// Deletes a group.
        /// Requires Administrator role.
        /// </summary>
        /// <param name="groupId">The ID of the group to delete.</param>
        /// <returns>NoContent if successful, NotFound if the group does not exist.</returns>
        [HttpDelete("{groupId}")]
        [Authorize(Roles = "Администратор")] // Only Administrator can delete groups
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var isDeleted = await _groupService.DeleteGroupAsync(groupId);

            if (!isDeleted)
            {
                // If deletion failed, assume it's because the group wasn't found.
                // If your service returns false for other reasons (e.g., group has associated students),
                // you might need more specific return types from the service to differentiate.
                return NotFound($"Group with ID {groupId} not found.");
            }

            return NoContent(); // 204 No Content for successful deletion
        }

        // --- Add other controller actions related to groups here ---
        // For example, getting students in a specific group.
        // [HttpGet("{groupId}/students")]
        // [Authorize(Roles = "Администратор,Преподаватель,Студент")]
        // public async Task<ActionResult<List<UserDto>>> GetStudentsByGroup(int groupId)
        // {
        //     // You would need a method in IGroupService (or IUserService) to retrieve students by group.
        //     // E.g., var students = await _groupService.GetStudentsInGroupAsync(groupId);
        //     // return Ok(students);
        // }
    }
}