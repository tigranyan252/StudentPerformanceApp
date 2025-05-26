// Path: StudentPerformance.Api/Services/GroupService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Data.Entities; // Assuming your Group entity is here
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace StudentPerformance.Api.Services
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GroupService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<GroupDto>> GetAllGroupsAsync()
        {
            var groups = await _context.Groups.ToListAsync();
            return _mapper.Map<IEnumerable<GroupDto>>(groups);
        }

        public async Task<GroupDto?> GetGroupByIdAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            return _mapper.Map<GroupDto>(group);
        }

        public async Task<GroupDto?> AddGroupAsync(AddGroupRequest request)
        {
            // Example: Check for duplicate group name before adding
            var existingGroup = await _context.Groups.FirstOrDefaultAsync(g => g.Name == request.Name);
            if (existingGroup != null)
            {
                return null; // Or throw a specific exception for duplicate name
            }

            var group = _mapper.Map<Group>(request);
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            return _mapper.Map<GroupDto>(group);
        }

        public async Task<bool> UpdateGroupAsync(int groupId, UpdateGroupRequest request)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                return false; // Group not found
            }

            // Example: Check for duplicate name if the name is being changed
            if (group.Name != request.Name)
            {
                var duplicateGroup = await _context.Groups.AnyAsync(g => g.Name == request.Name && g.GroupId != groupId);
                if (duplicateGroup)
                {
                    // This scenario should probably be handled by the controller returning BadRequest
                    // Or you could throw a custom exception here. For now, returning false.
                    return false;
                }
            }

            _mapper.Map(request, group); // Update entity properties from DTO

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Groups.AnyAsync(e => e.GroupId == groupId))
                {
                    return false; // Concurrency conflict: group was deleted by another process
                }
                throw; // Re-throw other concurrency exceptions
            }
            catch (Exception)
            {
                // Log exception
                return false; // General error during update
            }
        }

        public async Task<bool> DeleteGroupAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                return false; // Group not found
            }

            // Optional: Check for related entities (e.g., students in the group)
            // if you want to prevent deletion if there are dependencies.
            // if (await _context.Students.AnyAsync(s => s.GroupId == groupId)) { return false; } // Example

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}