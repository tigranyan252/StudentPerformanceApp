// Path: StudentPerformance.Api/Services/TeacherService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Добавлено: для ILogger
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces;
using StudentPerformance.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using StudentPerformance.Api.Exceptions; // Добавлено: для NotFoundException, ConflictException

namespace StudentPerformance.Api.Services
{
    /// <summary>
    /// Handles CRUD operations and retrieval of teachers, with integrated authorization checks
    /// by using IUserService for permission checks.
    /// </summary>
    public class TeacherService : ITeacherService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly ILogger<TeacherService> _logger; // Добавлено поле для логирования

        public TeacherService(ApplicationDbContext context, IMapper mapper, IUserService userService, ILogger<TeacherService> logger) // Обновлен конструктор
        {
            _context = context;
            _mapper = mapper;
            _userService = userService;
            _logger = logger; // Инициализация логгера
        }

        /// <summary>
        /// Retrieves a list of all teachers, with optional filtering by user name.
        /// Requires authorization via IUserService.
        /// </summary>
        public async Task<IEnumerable<TeacherDto>> GetAllTeachersAsync(string? userName, int currentUserId)
        {
            // Проверка авторизации: Только администратор может просматривать всех преподавателей.
            // ИСПОЛЬЗУЕМ МЕТОД ИЗ IUserService
            if (!await _userService.CanUserViewAllTeachersAsync(currentUserId))
            {
                _logger.LogWarning("Unauthorized access: User {UserId} attempted to view all teachers.", currentUserId);
                throw new UnauthorizedAccessException("Only administrators and teachers are authorized to view all teachers.");
            }

            var query = _context.Teachers
                .Include(t => t.User) // Включаем связанную сущность User
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userName))
            {
                query = query.Where(t =>
                    t.User != null &&
                    (t.User.Username.Contains(userName) ||
                     t.User.FirstName.Contains(userName) ||
                     t.User.LastName.Contains(userName)));
            }

            var teachers = await query.ToListAsync();
            _logger.LogInformation("Retrieved {Count} teachers. User ID: {UserId}.", teachers.Count(), currentUserId);
            return _mapper.Map<IEnumerable<TeacherDto>>(teachers);
        }

        /// <summary>
        /// Retrieves a specific teacher by ID.
        /// Requires authorization via IUserService.
        /// </summary>
        public async Task<TeacherDto?> GetTeacherByIdAsync(int teacherId, int currentUserId)
        {
            // Проверка авторизации: ИСПОЛЬЗУЕМ МЕТОД ИЗ IUserService
            if (!await _userService.CanUserViewTeacherDetailsAsync(currentUserId, teacherId))
            {
                _logger.LogWarning("Unauthorized access: User {UserId} attempted to view teacher details for Teacher ID {TeacherId}.", currentUserId, teacherId);
                throw new UnauthorizedAccessException("You are not authorized to view this teacher's profile.");
            }

            var teacher = await _context.Teachers
                .Include(t => t.User) // Включаем связанную сущность User
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            if (teacher == null)
            {
                _logger.LogWarning("Teacher with ID {TeacherId} not found after authorization check.", teacherId);
                return null; // Если не найден после авторизации, это 404
            }

            _logger.LogInformation("Teacher ID {TeacherId} retrieved. User ID: {UserId}.", teacherId, currentUserId);
            return _mapper.Map<TeacherDto>(teacher);
        }

        /// <summary>
        /// Adds a new teacher to the database.
        /// Requires authorization via IUserService.
        /// </summary>
        public async Task<TeacherDto?> AddTeacherAsync(AddTeacherRequest request, int currentUserId)
        {
            // Проверка авторизации: ИСПОЛЬЗУЕМ МЕТОД ИЗ IUserService
            if (!await _userService.CanUserAddTeacherAsync(currentUserId))
            {
                _logger.LogWarning("Unauthorized access: User {UserId} attempted to add a new teacher.", currentUserId);
                throw new UnauthorizedAccessException("Only administrators are authorized to add teachers.");
            }

            // 1. Check if the username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                _logger.LogWarning("Teacher registration failed: Username '{Username}' is already taken.", request.Username);
                throw new ConflictException($"Username '{request.Username}' is already taken."); // Используем ConflictException
            }

            // 2. Find Teacher Role
            var teacherRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.Teacher);
            if (teacherRole == null)
            {
                _logger.LogError("Critical error: Role '{RoleName}' not found in the database. Please seed roles.", UserRoles.Teacher);
                throw new InvalidOperationException($"Role '{UserRoles.Teacher}' not found in the database. Please seed roles.");
            }

            // 3. Create the User entity
            var newUser = _mapper.Map<User>(request);
            newUser.RoleId = teacherRole.RoleId;
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(); // Save user to get the new User.Id

            // 4. Create the Teacher entity, linking it to the new User
            var newTeacher = _mapper.Map<Teacher>(request);
            newTeacher.UserId = newUser.Id; // Link to the newly created user
            newTeacher.Department = request.Department;
            newTeacher.Position = request.Position;
            newTeacher.CreatedAt = DateTime.UtcNow;
            newTeacher.UpdatedAt = DateTime.UtcNow;

            _context.Teachers.Add(newTeacher);
            await _context.SaveChangesAsync();

            // 5. Load navigation properties for the DTO mapping
            await _context.Entry(newTeacher)
                .Reference(t => t.User).LoadAsync();
            if (newTeacher.User != null)
            {
                await _context.Entry(newTeacher.User)
                    .Reference(u => u.Role).LoadAsync();
            }

            _logger.LogInformation("Teacher '{Username}' (User ID: {UserId}, Teacher ID: {TeacherId}) added successfully by user {CurrentUserId}.", request.Username, newUser.Id, newTeacher.TeacherId, currentUserId);
            return _mapper.Map<TeacherDto>(newTeacher);
        }

        /// <summary>
        /// Updates an existing teacher's information.
        /// Requires authorization via IUserService.
        /// </summary>
        public async Task<bool> UpdateTeacherAsync(int teacherId, UpdateTeacherRequest request, int currentUserId)
        {
            // Проверка авторизации: ИСПОЛЬЗУЕМ МЕТОД ИЗ IUserService
            if (!await _userService.CanUserUpdateTeacherAsync(currentUserId, teacherId))
            {
                _logger.LogWarning("Unauthorized access: User {UserId} attempted to update teacher ID {TeacherId}.", currentUserId, teacherId);
                throw new UnauthorizedAccessException("You are not authorized to update this teacher's profile.");
            }

            var teacherToUpdate = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            if (teacherToUpdate == null || teacherToUpdate.User == null)
            {
                _logger.LogWarning("Teacher or associated user with ID {TeacherId} not found for update.", teacherId);
                // Бросаем NotFoundException, чтобы контроллер мог вернуть 404
                throw new NotFoundException($"Teacher with ID {teacherId} or associated user not found.");
            }

            // --- Update User properties ---
            if (!string.IsNullOrEmpty(request.Username) && teacherToUpdate.User.Username != request.Username)
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != teacherToUpdate.UserId))
                {
                    _logger.LogWarning("Update teacher failed: Username '{Username}' is already taken by another user (Teacher ID: {TeacherId}).", request.Username, teacherId);
                    // Бросаем ConflictException
                    throw new ConflictException($"Username '{request.Username}' is already taken by another user.");
                }
                teacherToUpdate.User.Username = request.Username;
            }
            if (!string.IsNullOrEmpty(request.FirstName))
            {
                teacherToUpdate.User.FirstName = request.FirstName;
            }
            if (!string.IsNullOrEmpty(request.LastName))
            {
                teacherToUpdate.User.LastName = request.LastName;
            }
            if (request.Email != null) // Allow setting to null explicitly if needed
            {
                teacherToUpdate.User.Email = request.Email;
            }
            teacherToUpdate.User.UpdatedAt = DateTime.UtcNow;


            // --- Update Teacher specific properties ---
            if (request.Department != null)
            {
                teacherToUpdate.Department = request.Department;
            }
            if (request.Position != null)
            {
                teacherToUpdate.Position = request.Position;
            }
            teacherToUpdate.UpdatedAt = DateTime.UtcNow;


            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Teacher {TeacherId} and associated user updated successfully by user {UserId}.", teacherId, currentUserId);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict occurred while updating teacher ID {TeacherId}. Checking if it still exists...", teacherId);
                if (!await _context.Teachers.AnyAsync(e => e.TeacherId == teacherId))
                {
                    _logger.LogWarning("Teacher {TeacherId} was deleted by another user during a concurrency conflict.", teacherId);
                    throw new NotFoundException($"Teacher with ID {teacherId} was deleted by another user.");
                }
                _logger.LogError(ex, "Concurrency conflict occurred while updating teacher ID {TeacherId}, but teacher still exists.", teacherId);
                throw new ConflictException($"Concurrency conflict when updating teacher with ID {teacherId}. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating teacher with ID {TeacherId}.", teacherId);
                // Бросаем более общее исключение или перебрасываем, чтобы контроллер обработал как 500
                throw;
            }
        }

        /// <summary>
        /// Deletes a teacher from the database.
        /// Requires authorization via IUserService.
        /// </summary>
        public async Task<bool> DeleteTeacherAsync(int teacherId, int currentUserId)
        {
            // Проверка авторизации: ИСПОЛЬЗУЕМ МЕТОД ИЗ IUserService
            if (!await _userService.CanUserDeleteTeacherAsync(currentUserId, teacherId))
            {
                _logger.LogWarning("Unauthorized access: User {UserId} attempted to delete teacher ID {TeacherId}.", currentUserId, teacherId);
                throw new UnauthorizedAccessException("You are not authorized to delete this teacher.");
            }

            var teacherToDelete = await _context.Teachers
                .Include(t => t.User) // Включаем User, чтобы удалить его
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            if (teacherToDelete == null)
            {
                _logger.LogWarning("Delete teacher failed: Teacher with ID {TeacherId} not found.", teacherId);
                // Бросаем NotFoundException
                throw new NotFoundException($"Teacher with ID {teacherId} not found.");
            }

            // Проверка зависимостей перед удалением
            var hasAssignments = await _context.TeacherSubjectGroupAssignments.AnyAsync(tsga => tsga.TeacherId == teacherId);
            if (hasAssignments)
            {
                _logger.LogWarning("Cannot delete teacher {TeacherId}: {Count} associated subject/group assignments exist.", teacherId, await _context.TeacherSubjectGroupAssignments.CountAsync(tsga => tsga.TeacherId == teacherId));
                // Бросаем ConflictException
                throw new ConflictException($"Cannot delete teacher with ID {teacherId} because they have associated subject/group assignments.");
            }

            var hasGrades = await _context.Grades.AnyAsync(g => g.TeacherId == teacherId); // Check if TeacherId is directly on Grade
            if (hasGrades)
            {
                _logger.LogWarning("Cannot delete teacher {TeacherId}: {Count} associated grades exist.", teacherId, await _context.Grades.CountAsync(g => g.TeacherId == teacherId));
                // Бросаем ConflictException
                throw new ConflictException($"Cannot delete teacher with ID {teacherId} because they have associated grades.");
            }

            _context.Teachers.Remove(teacherToDelete);

            // Также удаляем связанную запись User
            if (teacherToDelete.User != null)
            {
                _context.Users.Remove(teacherToDelete.User);
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Teacher {TeacherId} and associated user deleted successfully by user {UserId}.", teacherId, currentUserId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException occurred while deleting teacher ID {TeacherId}. Unhandled dependencies still exist. Details: {Message}", teacherId, ex.InnerException?.Message ?? ex.Message);
                throw new ConflictException($"Cannot delete teacher with ID {teacherId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting teacher with ID {TeacherId}.", teacherId);
                throw; // Re-throw the original exception
            }
        }
    }
}
