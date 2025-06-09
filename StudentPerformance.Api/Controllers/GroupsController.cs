// Path: StudentPerformance.Api/Controllers/GroupsController.cs

#nullable enable // ДОБАВЛЕНО: Включаем контекст nullable reference types

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces;
using System;
using static StudentPerformance.Api.Utilities.UserRoles;


namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IUserService _userService;

        public GroupsController(IGroupService groupService, IUserService userService)
        {
            _groupService = groupService;
            _userService = userService;
        }

        // Вспомогательный метод для получения ID текущего пользователя
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User ID claim not found or is invalid.");
        }

        [HttpGet]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")]
        public async Task<ActionResult<List<GroupDto>>> GetAllGroups(
            [FromQuery] string? name,
            [FromQuery] string? code)
        {
            int currentUserId = GetCurrentUserId();

            bool authorized = await _groupService.CanUserViewAllGroupsAsync(currentUserId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to view groups." });
            }

            var groups = await _groupService.GetAllGroupsAsync(name, code);
            return Ok(groups);
        }

        [HttpGet("{groupId}")]
        [Authorize(Roles = $"{Administrator},{Teacher},{Student}")]
        public async Task<ActionResult<GroupDto>> GetGroupById(int groupId)
        {
            int currentUserId = GetCurrentUserId();

            bool authorized = await _groupService.CanUserViewGroupDetailsAsync(currentUserId, groupId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to view this group's details." });
            }

            var group = await _groupService.GetGroupByIdAsync(groupId);

            if (group == null)
            {
                return NotFound();
            }
            return Ok(group);
        }

        [HttpPost]
        [Authorize(Roles = Administrator)]
        public async Task<IActionResult> AddGroup([FromBody] AddGroupRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int currentUserId = GetCurrentUserId();

            bool authorized = await _groupService.CanUserAddGroupAsync(currentUserId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to add groups." });
            }

            try
            {
                // ИСПРАВЛЕНО: Добавлен оператор null-forgiving (!) для addedGroupDto
                var addedGroupDto = await _groupService.AddGroupAsync(request);
                return CreatedAtAction(nameof(GetGroupById), new { groupId = addedGroupDto.GroupId }, addedGroupDto!);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while adding the group." });
            }
        }

        [HttpPut("{groupId}")]
        [Authorize(Roles = Administrator)]
        public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] UpdateGroupRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int currentUserId = GetCurrentUserId();

            bool authorized = await _groupService.CanUserUpdateGroupAsync(currentUserId, groupId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to update groups." });
            }

            try
            {
                var isUpdated = await _groupService.UpdateGroupAsync(groupId, request);
                if (!isUpdated)
                {
                    var existingGroup = await _groupService.GetGroupByIdAsync(groupId);
                    if (existingGroup == null)
                    {
                        return NotFound($"Group with ID {groupId} not found.");
                    }
                    return BadRequest("Failed to update group. Check for duplicate names/codes or other data conflicts.");
                }
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while updating the group." });
            }
        }

        [HttpDelete("{groupId}")]
        [Authorize(Roles = Administrator)]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            int currentUserId = GetCurrentUserId();

            bool authorized = await _groupService.CanUserDeleteGroupAsync(currentUserId, groupId);
            if (!authorized)
            {
                return StatusCode(403, new { message = "You are not authorized to delete groups." });
            }

            try
            {
                var isDeleted = await _groupService.DeleteGroupAsync(groupId);
                if (!isDeleted)
                {
                    return NotFound($"Group with ID {groupId} not found.");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the group." });
            }
        }
    }
}
