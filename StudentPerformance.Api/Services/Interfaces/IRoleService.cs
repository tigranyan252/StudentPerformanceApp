// Path: StudentPerformance.Api/Services/IRoleService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.DTOs; // <--- This using is crucial for the interface too!

namespace StudentPerformance.Api.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<RoleDto?> GetRoleByIdAsync(int id);
        Task<RoleDto?> CreateRoleAsync(CreateRoleDto createRoleDto);
        Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleDto updateRoleDto);
        Task<bool> DeleteRoleAsync(int id);
    }
}