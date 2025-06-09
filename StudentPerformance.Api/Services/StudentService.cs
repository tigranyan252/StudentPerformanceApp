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
using BCrypt.Net;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Utilities;
using Microsoft.Extensions.Logging;
using StudentPerformance.Api.Exceptions;
using StudentPerformance.Api.Services.Interfaces; // Добавлено для IStudentService

namespace StudentPerformance.Api.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StudentService> _logger;

        public StudentService(ApplicationDbContext context, IMapper mapper, ILogger<StudentService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<StudentDto>> GetAllStudentsAsync(int? groupId, string? userName)
        {
            _logger.LogInformation("Attempting to get all students with filters: GroupId={GroupId}, UserName={UserName}", (object)groupId, (object)userName);
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Group)
                .AsQueryable();

            if (groupId.HasValue && groupId.Value > 0)
            {
                query = query.Where(s => s.GroupId == groupId.Value);
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                query = query.Where(s => s.User != null && s.User.Username.Contains(userName));
            }

            var students = await query.ToListAsync();
            _logger.LogInformation("Retrieved {Count} students.", (object)students.Count);
            return _mapper.Map<List<StudentDto>>(students);
        }

        public async Task<StudentDto?> GetStudentByIdAsync(int studentId)
        {
            _logger.LogInformation("Attempting to get student by ID: {StudentId}", (object)studentId);
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                _logger.LogWarning("Student with ID {StudentId} not found.", (object)studentId);
            }
            else
            {
                _logger.LogInformation("Student with ID {StudentId} found.", (object)studentId);
            }
            return _mapper.Map<StudentDto>(student);
        }

        // НОВЫЙ МЕТОД: Реализация GetStudentByUserIdAsync
        public async Task<StudentDto?> GetStudentByUserIdAsync(int userId)
        {
            _logger.LogInformation("Attempting to retrieve student profile for UserId: {UserId}", (object)userId);
            var student = await _context.Students
                .Include(s => s.User) // Включаем данные пользователя
                .Include(s => s.Group) // Включаем данные группы
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogWarning("Student profile not found for UserId: {UserId}.", (object)userId);
                return null;
            }

            _logger.LogInformation("Student profile for UserId: {UserId} found (StudentId: {StudentId}).", (object)userId, (object)student.StudentId);
            return _mapper.Map<StudentDto>(student);
        }

        // НОВЫЙ МЕТОД: Реализация IsStudentInTeacherAssignedGroupAsync
        public async Task<bool> IsStudentInTeacherAssignedGroupAsync(int teacherId, int studentId)
        {
            _logger.LogInformation("Checking if student {StudentId} is in teacher {TeacherId}'s assigned groups.", (object)studentId, (object)teacherId);

            // Находим группу студента
            var studentGroupId = await _context.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.GroupId) // Проецируем GroupId как nullable int, чтобы использовать HasValue
                .FirstOrDefaultAsync();

            if (!studentGroupId.HasValue || studentGroupId.Value == 0) // Добавлена проверка на 0, если GroupId не nullable и 0 означает отсутствие группы
            {
                _logger.LogInformation("Student {StudentId} does not have a group assigned or group is 0. Returning false.", (object)studentId);
                return false;
            }

            // Проверяем, есть ли у преподавателя назначение (TeacherSubjectGroupAssignment)
            // для этой группы (studentGroupId.Value)
            var isAssigned = await _context.TeacherSubjectGroupAssignments
                .AnyAsync(tsga => tsga.TeacherId == teacherId && tsga.GroupId == studentGroupId.Value);

            _logger.LogInformation("Student {StudentId} is {IsAssigned} in teacher {TeacherId}'s assigned groups.", (object)studentId, isAssigned ? "TRUE" : "FALSE", (object)teacherId);
            return isAssigned;
        }


        public async Task<StudentDto?> AddStudentAsync(AddStudentRequest request)
        {
            _logger.LogInformation("Attempting to add new student with Username: {Username}", (object)request.Username);

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                _logger.LogWarning("AddStudentAsync failed: Username '{Username}' already taken.", (object)request.Username);
                throw new ConflictException("Имя пользователя уже занято.");
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                _logger.LogWarning("AddStudentAsync failed: Email '{Email}' already taken.", (object)request.Email);
                throw new ConflictException("Адрес электронной почты уже используется.");
            }

            if (!request.GroupId.HasValue || request.GroupId.Value == 0)
            {
                _logger.LogError("AddStudentAsync: GroupId is required for student but was not provided or is 0.");
                throw new BadRequestException("Для студента поле 'Группа' обязательно.");
            }
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == request.GroupId.Value);
            if (group == null)
            {
                _logger.LogWarning("AddStudentAsync failed: Group with ID {GroupId} not found.", (object)request.GroupId.Value);
                throw new NotFoundException($"Группа с ID {request.GroupId.Value} не найдена.");
            }

            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.Student);
            if (studentRole == null)
            {
                _logger.LogError("AddStudentAsync failed: Student role not found.");
                throw new InvalidOperationException("Роль 'Student' не найдена. Убедитесь, что роли заполнены в базе данных.");
            }

            var newUser = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                RoleId = studentRole.RoleId,
                Role = studentRole
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var newStudent = new Student
            {
                UserId = newUser.Id,
                GroupId = request.GroupId.Value,
                DateOfBirth = request.DateOfBirth,
                EnrollmentDate = request.EnrollmentDate,
                IsActive = true
            };

            _context.Students.Add(newStudent);
            await _context.SaveChangesAsync();

            await _context.Entry(newStudent)
                .Reference(s => s.User).LoadAsync();
            await _context.Entry(newStudent)
                .Reference(s => s.Group).LoadAsync();

            _logger.LogInformation("Student {StudentId} with User {UserId} added successfully.", (object)newStudent.StudentId, (object)newUser.Id);
            return _mapper.Map<StudentDto>(newStudent);
        }


        public async Task<bool> UpdateStudentAsync(int studentId, UpdateStudentRequest request)
        {
            _logger.LogInformation("Attempting to update student with ID: {StudentId}", (object)studentId);
            var studentToUpdate = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (studentToUpdate == null)
            {
                _logger.LogWarning("Update student failed: Student profile with ID {StudentId} not found.", (object)studentId);
                throw new NotFoundException($"Профиль студента с ID {studentId} не найден.");
            }

            if (studentToUpdate.User == null)
            {
                _logger.LogError("Update student failed for StudentId {StudentId}: Associated user is null. This indicates data inconsistency.", (object)studentId);
                throw new InvalidOperationException($"Связанный пользователь для студента с ID {studentId} отсутствует. Обратитесь к администратору.");
            }

            if (!string.IsNullOrEmpty(request.Username))
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != studentToUpdate.UserId))
                {
                    throw new ConflictException($"Имя пользователя '{request.Username}' уже занято.");
                }
                studentToUpdate.User.Username = request.Username;
            }
            if (!string.IsNullOrEmpty(request.FirstName))
            {
                studentToUpdate.User.FirstName = request.FirstName;
            }
            if (!string.IsNullOrEmpty(request.LastName))
            {
                studentToUpdate.User.LastName = request.LastName;
            }
            if (request.Email != null)
            {
                if (!string.IsNullOrWhiteSpace(request.Email) && await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != studentToUpdate.UserId))
                {
                    throw new ConflictException($"Адрес электронной почты '{request.Email}' уже используется другим пользователем.");
                }
                studentToUpdate.User.Email = request.Email;
            }

            studentToUpdate.DateOfBirth = request.DateOfBirth;
            studentToUpdate.EnrollmentDate = request.EnrollmentDate;

            if (request.GroupId.HasValue)
            {
                var newGroupExists = await _context.Groups.AnyAsync(g => g.GroupId == request.GroupId.Value);
                if (!newGroupExists)
                {
                    _logger.LogWarning("Update student failed for StudentId {StudentId}: Group with ID {GroupId} not found.", (object)studentId, (object)request.GroupId.Value);
                    throw new NotFoundException($"Группа с ID {request.GroupId.Value} не найдена.");
                }
                studentToUpdate.GroupId = request.GroupId.Value;
            }
            else
            {
                _logger.LogError("Update student failed for StudentId {StudentId}: GroupId is required but was not provided in the update request.", (object)studentId);
                throw new BadRequestException("Для студента поле 'Группа' обязательно.");
            }

            studentToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Student {StudentId} updated successfully.", (object)studentId);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict during student update for StudentId: {StudentId}", (object)studentId);
                if (!await _context.Students.AnyAsync(e => e.StudentId == studentId))
                {
                    throw new NotFoundException($"Профиль студента с ID {studentId} не найден (возможно, был удален другим пользователем).");
                }
                throw new ConflictException($"Конфликт одновременного доступа при обновлении студента с ID {studentId}. Пожалуйста, попробуйте еще раз.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student with ID: {StudentId}", (object)studentId);
                throw new Exception($"Произошла неожиданная ошибка при обновлении студента с ID {studentId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteStudentAsync(int studentId)
        {
            _logger.LogInformation("Attempting to delete student with ID: {StudentId}", (object)studentId);
            var studentToDelete = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Grades)
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (studentToDelete == null)
            {
                _logger.LogWarning("Delete student failed: Student with ID {StudentId} not found.", (object)studentId);
                throw new NotFoundException($"Student with ID {studentId} not found.");
            }

            if (studentToDelete.Grades != null && studentToDelete.Grades.Any())
            {
                _logger.LogWarning("Cannot delete student {StudentId}: {Count} associated grades exist.", (object)studentId, (object)studentToDelete.Grades.Count);
                throw new ConflictException($"Cannot delete student with ID {studentId} because {studentToDelete.Grades.Count} associated grades exist.");
            }

            if (studentToDelete.Attendances != null && studentToDelete.Attendances.Any())
            {
                _logger.LogWarning("Cannot delete student {StudentId}: {Count} associated attendance records exist.", (object)studentId, (object)studentToDelete.Attendances.Count);
                throw new ConflictException($"Cannot delete student with ID {studentId} because {studentToDelete.Attendances.Count} associated attendance records exist.");
            }

            _context.Students.Remove(studentToDelete);
            if (studentToDelete.User != null)
            {
                _context.Users.Remove(studentToDelete.User);
                _logger.LogInformation("Associated user {UserId} for student {StudentId} marked for deletion.", (object)studentToDelete.User.Id, (object)studentId);
            }
            else
            {
                _logger.LogWarning("Student with ID {StudentId} does not have an associated user to delete (User property is null).", (object)studentId);
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Student {StudentId} and associated user deleted successfully.", (object)studentId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException occurred while deleting student {StudentId}. Unhandled dependencies still exist. Details: {Message}", (object)studentId, (object)(ex.InnerException?.Message ?? ex.Message));
                throw new ConflictException($"Cannot delete student with ID {studentId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during deletion of student {StudentId}. Details: {Message}", (object)studentId, (object)ex.Message);
                throw new Exception($"An unexpected error occurred while deleting student with ID {studentId}: {ex.Message}", ex);
            }
        }

        // --- Методы авторизации (CanUser...) ---
        // Эти методы должны быть перенесены в UserService, если это основной сервис для авторизации
        // или находиться в отдельном сервисе авторизации/разрешений.
        // Я оставляю их здесь в соответствии с вашим предоставленным кодом, но это может привести
        // к дублированию логики с UserService.
        public async Task<bool> CanUserViewAllStudentsAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return user.Role?.Name == UserRoles.Administrator || user.Role?.Name == UserRoles.Teacher;
        }

        public async Task<bool> CanUserViewStudentDetailsAsync(int userId, int studentId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return false;

            if (currentUser.Role?.Name == UserRoles.Administrator || currentUser.Role?.Name == UserRoles.Teacher)
            {
                return true;
            }

            if (currentUser.Role?.Name == UserRoles.Student && currentUser.Id == student.UserId)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> CanUserAddStudentAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return user.Role?.Name == UserRoles.Administrator;
        }

        public async Task<bool> CanUserUpdateStudentAsync(int userId, int studentId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return false;

            if (currentUser.Role?.Name == UserRoles.Administrator)
            {
                return true;
            }

            if (currentUser.Role?.Name == UserRoles.Student && currentUser.Id == student.UserId)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> CanUserDeleteStudentAsync(int userId, int studentId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return false;

            if (currentUser.Role?.Name == UserRoles.Administrator)
            {
                return true;
            }

            if (currentUser.Role?.Name == UserRoles.Student && currentUser.Id == student.UserId)
            {
                // return true; // Keep commented as per previous discussion, students shouldn't delete their own accounts from admin panel
            }

            return false;
        }
    }
}
