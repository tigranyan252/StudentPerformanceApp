// Path: StudentPerformance.Api/Services/GroupService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces; // ИСПРАВЛЕНО: Правильный namespace для интерфейса
using StudentPerformance.Api.Utilities; // ДОБАВЛЕНО: Для доступа к константам ролей
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services // ИСПРАВЛЕНО: Namespace остается Services для реализации
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

        // ИСПРАВЛЕНО: Добавлены параметры для фильтрации и возвращаемый тип List<GroupDto>
        public async Task<List<GroupDto>> GetAllGroupsAsync(string? name, string? code)
        {
            var query = _context.Groups.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(g => g.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                query = query.Where(g => g.Code.Contains(code));
            }

            var groups = await query.ToListAsync();
            return _mapper.Map<List<GroupDto>>(groups);
        }

        public async Task<GroupDto?> GetGroupByIdAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            return _mapper.Map<GroupDto>(group);
        }

        public async Task<GroupDto> AddGroupAsync(AddGroupRequest request)
        {
            // Check if a group with the same name or code already exists
            if (await _context.Groups.AnyAsync(g => g.Name == request.Name))
            {
                throw new ArgumentException("A group with the same name already exists.");
            }
            if (await _context.Groups.AnyAsync(g => g.Code == request.Code))
            {
                throw new ArgumentException("A group with the same code already exists.");
            }

            var newGroup = _mapper.Map<Group>(request);
            newGroup.CreatedAt = DateTime.UtcNow;
            newGroup.UpdatedAt = DateTime.UtcNow;

            _context.Groups.Add(newGroup);
            await _context.SaveChangesAsync();

            return _mapper.Map<GroupDto>(newGroup);
        }

        public async Task<bool> UpdateGroupAsync(int groupId, UpdateGroupRequest request)
        {
            var groupToUpdate = await _context.Groups.FindAsync(groupId);
            if (groupToUpdate == null)
            {
                return false;
            }

            // Check for duplicate name or code if they are being updated
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != groupToUpdate.Name)
            {
                if (await _context.Groups.AnyAsync(g => g.Name == request.Name && g.GroupId != groupId))
                {
                    throw new ArgumentException("Another group with the same name already exists.");
                }
                groupToUpdate.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != groupToUpdate.Code)
            {
                if (await _context.Groups.AnyAsync(g => g.Code == request.Code && g.GroupId != groupId))
                {
                    throw new ArgumentException("Another group with the same code already exists.");
                }
                groupToUpdate.Code = request.Code;
            }

            // Update description if provided
            if (request.Description != null) // Allow setting to null or empty string
            {
                groupToUpdate.Description = request.Description;
            }

            groupToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Groups.Any(e => e.GroupId == groupId))
                {
                    return false; // Not found (deleted by another user or didn't exist initially)
                }
                throw; // Re-throw if it's a genuine concurrency issue
            }
            catch (Exception)
            {
                // Log the exception here
                return false; // Failed for other reasons
            }
        }

        public async Task<bool> DeleteGroupAsync(int groupId)
        {
            var groupToDelete = await _context.Groups
                .Include(g => g.Students) // Include students to check for dependencies
                .Include(g => g.TeacherSubjectGroupAssignments) // Include assignments to check for dependencies
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (groupToDelete == null) return false;

            // Check for dependencies:
            // 1. Students associated with this group
            if (groupToDelete.Students.Any())
            {
                throw new InvalidOperationException("Cannot delete group: There are students associated with this group.");
            }

            // 2. TeacherSubjectGroupAssignments associated with this group
            if (groupToDelete.TeacherSubjectGroupAssignments.Any())
            {
                throw new InvalidOperationException("Cannot delete group: There are teacher-subject-group assignments associated with this group.");
            }

            _context.Groups.Remove(groupToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Authorization/Permission Checks for Groups (ДОБАВЛЕНО) ---

        public async Task<bool> CanUserViewAllGroupsAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return user.Role?.Name == UserRoles.Administrator || user.Role?.Name == UserRoles.Teacher || user.Role?.Name == UserRoles.Student;
        }

        public async Task<bool> CanUserViewGroupDetailsAsync(int userId, int groupId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            // Admins can view any group
            if (currentUser.Role?.Name == UserRoles.Administrator)
            {
                return true;
            }

            // Teachers can view any group
            if (currentUser.Role?.Name == UserRoles.Teacher)
            {
                return true;
            }

            // Students can view their own group details
            if (currentUser.Role?.Name == UserRoles.Student)
            {
                var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null && student.GroupId == groupId)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> CanUserAddGroupAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return user.Role?.Name == UserRoles.Administrator;
        }

        public async Task<bool> CanUserUpdateGroupAsync(int userId, int groupId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            // Only administrators can update groups
            return currentUser.Role?.Name == UserRoles.Administrator;
        }

        public async Task<bool> CanUserDeleteGroupAsync(int userId, int groupId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            // Only administrators can delete groups
            return currentUser.Role?.Name == UserRoles.Administrator;
        }
    }
}
