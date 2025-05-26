// Path: StudentPerformance.Api/Services/StudentService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.QueryParameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// Don't forget BCrypt.Net if it's not already referenced
using BCrypt.Net; // Add this using directive if you're using BCrypt for password hashing

namespace StudentPerformance.Api.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public StudentService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<StudentDto>> GetAllStudentsAsync(StudentQueryParameters? queryParameters = null)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Group)
                .AsQueryable();

            // Apply filtering, sorting, pagination if queryParameters are used
            // if (queryParameters != null) { ... apply filters/sort/pagination ... }

            var students = await query.ToListAsync();
            return _mapper.Map<List<StudentDto>>(students);
        }

        public async Task<StudentDto?> GetStudentByIdAsync(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            return _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto?> AddStudentAsync(AddStudentRequest request)
        {
            // First, create the User part
            var user = _mapper.Map<User>(request);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("DefaultPassword123!"); // Example: Set a default or temporary password
            // Find Student Role
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Студент");
            if (studentRole == null)
            {
                throw new InvalidOperationException("Student role not found. Please ensure roles are seeded.");
            }
            user.RoleId = studentRole.RoleId;
            user.Role = studentRole;

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Save user to get UserId (or Id)

            // Then, create the Student part
            var student = _mapper.Map<Student>(request);
            student.UserId = user.Id; // <<< FIX: Changed user.UserId to user.Id
            student.User = user;

            // Check if GroupId exists
            if (request.GroupId.HasValue) // <<< This implies request.GroupId needs to be int? in DTO
            {
                var group = await _context.Groups.FindAsync(request.GroupId.Value); // <<< This implies request.GroupId needs to be int? in DTO
                if (group == null)
                {
                    return null; // Indicate group not found
                }
                student.Group = group;
            }

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return _mapper.Map<StudentDto>(student);
        }

        public async Task<bool> UpdateStudentAsync(int studentId, UpdateStudentRequest request)
        {
            var studentToUpdate = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (studentToUpdate == null) return false;

            // Update User properties
            if (studentToUpdate.User != null)
            {
                studentToUpdate.User.FirstName = request.FullName;
                studentToUpdate.User.Email = request.Email;
            }

            // Update Student specific properties
            _mapper.Map(request, studentToUpdate);

            // Handle GroupId update
            if (request.GroupId.HasValue) // <<< This implies request.GroupId needs to be int? in DTO
            {
                var newGroup = await _context.Groups.FindAsync(request.GroupId.Value); // <<< This implies request.GroupId needs to be int? in DTO
                if (newGroup == null) return false;
                studentToUpdate.GroupId = request.GroupId.Value;
                studentToUpdate.Group = newGroup;
            }
            else
            {
                studentToUpdate.GroupId = null;
                studentToUpdate.Group = null;
            }

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }

        public async Task<bool> DeleteStudentAsync(int studentId)
        {
            var studentToDelete = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (studentToDelete == null) return false;

            _context.Students.Remove(studentToDelete);
            if (studentToDelete.User != null)
            {
                _context.Users.Remove(studentToDelete.User);
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUserViewAllStudentsAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId); // <<< FIX: Changed u.UserId to u.Id
            if (user == null) return false;
            return user.Role?.Name == "Администратор" || user.Role?.Name == "Преподаватель";
        }

        public async Task<bool> CanUserViewStudentDetailsAsync(int userId, int studentId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId); // <<< FIX: Changed u.UserId to u.Id
            if (currentUser == null) return false;

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return false;

            if (currentUser.Role?.Name == "Администратор" || currentUser.Role?.Name == "Преподаватель")
            {
                return true;
            }

            if (currentUser.Role?.Name == "Студент" && currentUser.Id == student.UserId) // <<< FIX: Changed currentUser.UserId to currentUser.Id
            {
                return true;
            }

            return false;
        }

        public async Task<bool> CanUserAddStudentAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId); // <<< FIX: Changed u.UserId to u.Id
            if (user == null) return false;
            return user.Role?.Name == "Администратор";
        }

        public async Task<bool> CanUserUpdateStudentAsync(int userId, int studentId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId); // <<< FIX: Changed u.UserId to u.Id
            if (currentUser == null) return false;

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return false;

            if (currentUser.Role?.Name == "Администратор")
            {
                return true;
            }

            if (currentUser.Role?.Name == "Студент" && currentUser.Id == student.UserId) // <<< FIX: Changed currentUser.UserId to currentUser.Id
            {
                return true;
            }

            return false;
        }

        public async Task<bool> CanUserDeleteStudentAsync(int userId, int studentId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId); // <<< FIX: Changed u.UserId to u.Id
            if (currentUser == null) return false;

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return false;

            if (currentUser.Role?.Name == "Администратор")
            {
                return true;
            }

            if (currentUser.Role?.Name == "Студент" && currentUser.Id == student.UserId) // <<< FIX: Changed currentUser.UserId to currentUser.Id
            {
                // Typically, students should not be able to delete their own accounts from the system.
                // This might be a policy decision. If they can, uncomment below.
                // return true;
            }

            return false;
        }
    }
}