// Path: Services/RoleService.cs

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Models.DTOs; // <--- ADD THIS LINE to find your DTOs

namespace StudentPerformance.Api.Services
{
    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _dbContext;

        public RoleService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            // Direct projection from entity to DTO using Select
            return await _dbContext.Roles
                                   .Select(r => new RoleDto
                                   {
                                       RoleId = r.RoleId,
                                       Name = r.Name,
                                       Description = r.Description
                                   })
                                   .ToListAsync();
        }

        public async Task<RoleDto?> GetRoleByIdAsync(int id)
        {
            var role = await _dbContext.Roles.FindAsync(id);

            if (role == null)
            {
                return null;
            }

            // Manually map entity to DTO
            return new RoleDto
            {
                RoleId = role.RoleId,
                Name = role.Name,
                Description = role.Description
            };
        }

        public async Task<RoleDto?> CreateRoleAsync(CreateRoleDto createRoleDto)
        {
            // Check for duplicate name
            if (await _dbContext.Roles.AnyAsync(r => r.Name == createRoleDto.Name))
            {
                return null; // Return null if a role with this name already exists
            }

            var role = new Role
            {
                Name = createRoleDto.Name,
                Description = createRoleDto.Description
            };

            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync();

            // Return the DTO of the newly created role
            return new RoleDto
            {
                RoleId = role.RoleId,
                Name = role.Name,
                Description = role.Description
            };
        }

        public async Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleDto updateRoleDto)
        {
            var roleToUpdate = await _dbContext.Roles.FindAsync(id);

            if (roleToUpdate == null)
            {
                return null; // Role not found
            }

            // Check for duplicate name if the name is being changed
            if (roleToUpdate.Name != updateRoleDto.Name &&
                await _dbContext.Roles.AnyAsync(r => r.Name == updateRoleDto.Name && r.RoleId != id))
            {
                return null; // A different role with the new name already exists
            }

            roleToUpdate.Name = updateRoleDto.Name;
            roleToUpdate.Description = updateRoleDto.Description;

            await _dbContext.SaveChangesAsync();

            // Return the DTO of the updated role
            return new RoleDto
            {
                RoleId = roleToUpdate.RoleId,
                Name = roleToUpdate.Name,
                Description = roleToUpdate.Description
            };
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            var roleToDelete = await _dbContext.Roles.FindAsync(id);

            if (roleToDelete == null)
            {
                return false; // Role not found
            }

            // Optional: Check if there are any users associated with this role
            // if you want to prevent deletion of roles that are still in use.
            // For example:
            // if (await _dbContext.Users.AnyAsync(u => u.RoleId == id))
            // {
            //     // You might return false, or throw a specific exception here
            //     // indicating that the role cannot be deleted due to dependencies.
            //     return false;
            // }

            _dbContext.Roles.Remove(roleToDelete);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}