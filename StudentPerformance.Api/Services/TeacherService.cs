// Path: StudentPerformance.Api/Services/TeacherService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net; // Don't forget this if you hash passwords

namespace StudentPerformance.Api.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TeacherService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<TeacherDto>> GetAllTeachersAsync()
        {
            var query = _context.Teachers
                .Include(t => t.User)
                .AsQueryable();

            var teachers = await query.ToListAsync();
            return _mapper.Map<List<TeacherDto>>(teachers);
        }

        public async Task<TeacherDto?> GetTeacherByIdAsync(int teacherId)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            return _mapper.Map<TeacherDto>(teacher);
        }

        public async Task<TeacherDto?> AddTeacherAsync(AddTeacherRequest request)
        {
            // First, map the DTO request to a User entity.
            // AutoMapper will handle splitting FullName into FirstName and LastName,
            // and mapping Login from DTO to Username in User entity, based on MappingProfile.
            var user = _mapper.Map<User>(request);

            // It's crucial to hash the password here.
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Find Teacher Role
            var teacherRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Преподаватель");
            if (teacherRole == null)
            {
                throw new InvalidOperationException("Teacher role not found. Please ensure roles are seeded.");
            }
            user.RoleId = teacherRole.RoleId;
            user.Role = teacherRole; // Assign the navigation property if needed for immediate use, otherwise just RoleId is fine.

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Save user to get UserId (or Id)

            // Then, create the Teacher part
            var teacher = _mapper.Map<Teacher>(request);
            teacher.UserId = user.Id; // Link Teacher to the newly created User
            teacher.User = user; // Assign navigation property

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            return _mapper.Map<TeacherDto>(teacher);
        }

        public async Task<bool> UpdateTeacherAsync(int teacherId, UpdateTeacherRequest request)
        {
            var teacherToUpdate = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            if (teacherToUpdate == null) return false;

            // Update User properties using AutoMapper
            // AutoMapper will handle splitting FullName into FirstName and LastName for User entity
            // and mapping Email. It will ignore Username/Password as they are not in UpdateTeacherRequest.
            if (teacherToUpdate.User != null)
            {
                _mapper.Map(request, teacherToUpdate.User);
                // Do NOT update password here unless you have specific password change functionality
            }

            // Update Teacher specific properties
            // AutoMapper will map other relevant properties from UpdateTeacherRequest to Teacher entity
            _mapper.Map(request, teacherToUpdate);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // This typically means the entity was updated/deleted by another process.
                // Could also indicate other SaveChanges failures if not handled more specifically.
                return false;
            }
        }

        public async Task<bool> DeleteTeacherAsync(int teacherId)
        {
            var teacherToDelete = await _context.Teachers
                .Include(t => t.User) // Include User to delete it along with the teacher
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            if (teacherToDelete == null) return false;

            _context.Teachers.Remove(teacherToDelete);

            // Also remove the associated User record
            if (teacherToDelete.User != null)
            {
                _context.Users.Remove(teacherToDelete.User);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // --- Authorization/Permission Checks for Teachers ---

        public async Task<bool> CanUserViewAllTeachersAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return user.Role?.Name == "Администратор" || user.Role?.Name == "Преподаватель";
        }

        public async Task<bool> CanUserViewTeacherDetailsAsync(int userId, int teacherId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            var teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.TeacherId == teacherId);
            if (teacher == null) return false;

            if (currentUser.Role?.Name == "Администратор" || currentUser.Role?.Name == "Преподаватель")
            {
                // Teachers can view all teachers, or specific teacher details (including their own)
                // If it's the current user, they can view their own details.
                return true;
            }

            // Only administrators and teachers (including themselves) can view teacher details.
            return false;
        }

        public async Task<bool> CanUserAddTeacherAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return user.Role?.Name == "Администратор";
        }

        public async Task<bool> CanUserUpdateTeacherAsync(int userId, int teacherId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            var teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.TeacherId == teacherId);
            if (teacher == null) return false; // Must exist to update

            if (currentUser.Role?.Name == "Администратор")
            {
                return true;
            }

            // A teacher can update their *own* profile
            if (currentUser.Role?.Name == "Преподаватель" && currentUser.Id == teacher.UserId)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> CanUserDeleteTeacherAsync(int userId, int teacherId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            var teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.TeacherId == teacherId);
            if (teacher == null) return false; // Must exist to delete

            if (currentUser.Role?.Name == "Администратор")
            {
                return true;
            }

            // Teachers generally should not be able to delete other teachers or themselves.
            // This is a policy decision. Defaulting to false for non-admins.
            return false;
        }
    }
}