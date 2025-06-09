// Path: StudentPerformance.Api/Services/Interfaces/IGroupService.cs

using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services.Interfaces // ИСПРАВЛЕНО: Правильный namespace
{
    public interface IGroupService
    {
        // ИСПРАВЛЕНО: Добавлены параметры для фильтрации и возвращаемый тип List<GroupDto>
        Task<List<GroupDto>> GetAllGroupsAsync(string? name, string? code);
        Task<GroupDto?> GetGroupByIdAsync(int groupId);
        Task<GroupDto> AddGroupAsync(AddGroupRequest request);
        Task<bool> UpdateGroupAsync(int groupId, UpdateGroupRequest request);
        Task<bool> DeleteGroupAsync(int groupId);

        // ДОБАВЛЕНО: Методы авторизации/проверки разрешений для групп
        Task<bool> CanUserViewAllGroupsAsync(int userId);
        Task<bool> CanUserViewGroupDetailsAsync(int userId, int groupId);
        Task<bool> CanUserAddGroupAsync(int userId);
        Task<bool> CanUserUpdateGroupAsync(int userId, int groupId);
        Task<bool> CanUserDeleteGroupAsync(int userId, int groupId);
    }
}
