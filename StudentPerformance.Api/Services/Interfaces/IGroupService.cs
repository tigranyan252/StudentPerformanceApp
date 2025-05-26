// Path: StudentPerformance.Api/Services/IGroupService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.DTOs;

namespace StudentPerformance.Api.Services
{
    public interface IGroupService
    {
        Task<IEnumerable<GroupDto>> GetAllGroupsAsync();
        Task<GroupDto?> GetGroupByIdAsync(int groupId);
        Task<GroupDto?> AddGroupAsync(AddGroupRequest request);
        Task<bool> UpdateGroupAsync(int groupId, UpdateGroupRequest request);
        Task<bool> DeleteGroupAsync(int groupId);
        // You might add a method like Task<bool> DoesGroupExistAsync(int groupId);
        // or Task<bool> IsGroupNameUniqueAsync(string groupName, int? excludeGroupId = null);
        // if your service handles these specific checks for the controller.
    }
}