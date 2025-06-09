// Path: StudentPerformance.Api/Services/SubjectService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces; // ИСПРАВЛЕНО: Правильный namespace для интерфейса
using StudentPerformance.Api.Utilities; // ДОБАВЛЕНО: Для доступа к константам ролей
using System; // ДОБАВЛЕНО: Для ArgumentException, InvalidOperationException
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SubjectService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ИСПРАВЛЕНО: Добавлены параметры name и code для фильтрации, как в ISubjectService
        public async Task<List<SubjectDto>> GetAllSubjectsAsync(string? name, string? code)
        {
            var query = _context.Subjects.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(s => s.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                query = query.Where(s => s.Code.Contains(code));
            }

            var subjects = await query.ToListAsync();
            return _mapper.Map<List<SubjectDto>>(subjects);
        }

        public async Task<SubjectDto?> GetSubjectByIdAsync(int subjectId)
        {
            var subject = await _context.Subjects.FindAsync(subjectId); // Используем FindAsync для поиска по PK
            return _mapper.Map<SubjectDto>(subject);
        }

        // ИСПРАВЛЕНО: Возвращаемый тип изменен на Task<SubjectDto> (без nullable)
        // ИСПРАВЛЕНО: Добавлены проверки на уникальность кода и обработка исключений
        public async Task<SubjectDto> AddSubjectAsync(AddSubjectRequest request)
        {
            // Проверка на существование предмета с таким же именем или кодом
            if (await _context.Subjects.AnyAsync(s => s.Name == request.Name))
            {
                throw new ArgumentException("A subject with the same name already exists.");
            }
            if (await _context.Subjects.AnyAsync(s => s.Code == request.Code))
            {
                throw new ArgumentException("A subject with the same code already exists.");
            }

            var newSubject = _mapper.Map<Subject>(request);
            newSubject.CreatedAt = DateTime.UtcNow; // Установка даты создания
            newSubject.UpdatedAt = DateTime.UtcNow; // Установка даты обновления

            _context.Subjects.Add(newSubject);
            await _context.SaveChangesAsync();

            return _mapper.Map<SubjectDto>(newSubject);
        }

        // ИСПРАВЛЕНО: Добавлены проверки на уникальность кода при обновлении и обработка исключений
        // ИСПРАВЛЕНО: Обновление поля Description
        public async Task<bool> UpdateSubjectAsync(int subjectId, UpdateSubjectRequest request)
        {
            var subjectToUpdate = await _context.Subjects.FindAsync(subjectId);
            if (subjectToUpdate == null)
            {
                return false;
            }

            // Проверка на дубликаты имени или кода, если они обновляются
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != subjectToUpdate.Name)
            {
                if (await _context.Subjects.AnyAsync(s => s.Name == request.Name && s.SubjectId != subjectId))
                {
                    throw new ArgumentException("Another subject with the same name already exists.");
                }
                subjectToUpdate.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != subjectToUpdate.Code)
            {
                if (await _context.Subjects.AnyAsync(s => s.Code == request.Code && s.SubjectId != subjectId))
                {
                    throw new ArgumentException("Another subject with the same code already exists.");
                }
                subjectToUpdate.Code = request.Code;
            }

            // Обновление описания, если предоставлено (позволяет установить null или пустую строку)
            if (request.Description != null)
            {
                subjectToUpdate.Description = request.Description;
            }

            subjectToUpdate.UpdatedAt = DateTime.UtcNow; // Обновление даты изменения

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Subjects.Any(e => e.SubjectId == subjectId))
                {
                    return false; // Не найдено (удалено другим пользователем или не существовало изначально)
                }
                throw; // Перевыбросить, если это настоящая проблема параллелизма
            }
            catch (Exception)
            {
                // Залогировать исключение здесь (например, через ILogger)
                return false; // Сбой по другим причинам
            }
        }

        // ИСПРАВЛЕНО: Добавлена проверка зависимостей перед удалением
        public async Task<bool> DeleteSubjectAsync(int subjectId)
        {
            var subjectToDelete = await _context.Subjects
                .Include(s => s.TeacherSubjectGroupAssignments) // Включить назначения для проверки зависимостей
                .FirstOrDefaultAsync(s => s.SubjectId == subjectId);

            if (subjectToDelete == null) return false;

            // Проверка зависимостей:
            // 1. TeacherSubjectGroupAssignments, связанные с этим предметом
            if (subjectToDelete.TeacherSubjectGroupAssignments.Any())
            {
                throw new InvalidOperationException("Cannot delete subject: There are teacher-subject-group assignments associated with this subject.");
            }

            _context.Subjects.Remove(subjectToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Authorization/Permission Checks for Subjects ---

        public async Task<bool> CanUserViewAllSubjectsAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            // ИСПРАВЛЕНО: Используем константы ролей из Utilities.UserRoles
            return user.Role?.Name == UserRoles.Administrator || user.Role?.Name == UserRoles.Teacher || user.Role?.Name == UserRoles.Student;
        }

        public async Task<bool> CanUserViewSubjectDetailsAsync(int userId, int subjectId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            // Проверяем, существует ли предмет
            if (!await _context.Subjects.AnyAsync(s => s.SubjectId == subjectId))
            {
                return false;
            }

            // Администраторы и преподаватели могут просматривать любой предмет
            if (currentUser.Role?.Name == UserRoles.Administrator || currentUser.Role?.Name == UserRoles.Teacher)
            {
                return true;
            }

            // Студенты могут просматривать предметы, которые им назначены (через TeacherSubjectGroupAssignment)
            if (currentUser.Role?.Name == UserRoles.Student)
            {
                var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    var hasAssignment = await _context.TeacherSubjectGroupAssignments
                        .AnyAsync(tsga => tsga.SubjectId == subjectId && tsga.GroupId == student.GroupId);
                    return hasAssignment;
                }
            }

            return false;
        }

        public async Task<bool> CanUserAddSubjectAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            // ИСПРАВЛЕНО: Используем константу роли
            return user.Role?.Name == UserRoles.Administrator;
        }

        public async Task<bool> CanUserUpdateSubjectAsync(int userId, int subjectId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            // Проверяем, существует ли предмет
            if (!await _context.Subjects.AnyAsync(s => s.SubjectId == subjectId))
            {
                return false;
            }
            // ИСПРАВЛЕНО: Используем константу роли
            return currentUser.Role?.Name == UserRoles.Administrator;
        }

        public async Task<bool> CanUserDeleteSubjectAsync(int userId, int subjectId)
        {
            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null) return false;

            // Проверяем, существует ли предмет
            if (!await _context.Subjects.AnyAsync(s => s.SubjectId == subjectId))
            {
                return false;
            }
            // ИСПРАВЛЕНО: Используем константу роли
            return currentUser.Role?.Name == UserRoles.Administrator;
        }
    }
}
