// Path: StudentPerformance.Api/Services/SemesterService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Exceptions;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces;
using StudentPerformance.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SemesterService> _logger;

        public SemesterService(ApplicationDbContext context, IMapper mapper, ILogger<SemesterService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // ИЗМЕНЕНО: Метод теперь принимает параметры для фильтрации
        public async Task<IEnumerable<SemesterDto>> GetAllSemestersAsync(string? name = null, string? code = null, DateTime? startDateFrom = null, DateTime? endDateTo = null)
        {
            _logger.LogInformation($"Fetching all semesters with filters: Name='{name}', Code='{code}', StartDateFrom='{startDateFrom}', EndDateTo='{endDateTo}'");

            var query = _context.Semesters.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(s => s.Name.Contains(name));
            }

            // ДОБАВЛЕНО: Фильтрация по коду
            if (!string.IsNullOrWhiteSpace(code))
            {
                query = query.Where(s => s.Code != null && s.Code.Contains(code));
            }

            // ДОБАВЛЕНО: Фильтрация по дате начала (с)
            if (startDateFrom.HasValue)
            {
                // Сравниваем только часть даты без времени, чтобы захватить весь день
                query = query.Where(s => s.StartDate.Date >= startDateFrom.Value.Date);
            }

            // ДОБАВЛЕНО: Фильтрация по дате окончания (по)
            if (endDateTo.HasValue)
            {
                // Сравниваем только часть даты без времени, чтобы захватить весь день
                query = query.Where(s => s.EndDate.Date <= endDateTo.Value.Date);
            }

            var semesters = await query.ToListAsync();
            return _mapper.Map<IEnumerable<SemesterDto>>(semesters);
        }

        public async Task<SemesterDto?> GetSemesterByIdAsync(int semesterId)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);
            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task<SemesterDto> AddSemesterAsync(AddSemesterRequest request)
        {
            // ИЗМЕНЕНО: Проверка на дубликат названия ИЛИ КОДА
            if (await _context.Semesters.AnyAsync(s => s.Name == request.Name))
            {
                _logger.LogWarning($"Add semester failed: Semester with name '{request.Name}' already exists.");
                throw new ConflictException($"Семестр с названием '{request.Name}' уже существует.");
            }
            if (!string.IsNullOrWhiteSpace(request.Code) && await _context.Semesters.AnyAsync(s => s.Code == request.Code))
            {
                _logger.LogWarning($"Add semester failed: Semester with code '{request.Code}' already exists.");
                throw new ConflictException($"Семестр с кодом '{request.Code}' уже существует.");
            }


            var semester = _mapper.Map<Semester>(request);
            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();
            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task<bool> UpdateSemesterAsync(int semesterId, UpdateSemesterRequest request)
        {
            var semesterToUpdate = await _context.Semesters.FindAsync(semesterId);
            if (semesterToUpdate == null)
            {
                throw new NotFoundException($"Семестр с ID {semesterId} не найден для обновления.");
            }

            // ИЗМЕНЕНО: Проверка на дубликат названия с выбросом ConflictException (учитывая, что это другой семестр)
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != semesterToUpdate.Name)
            {
                if (await _context.Semesters.AnyAsync(s => s.Name == request.Name && s.SemesterId != semesterId))
                {
                    _logger.LogWarning($"Update semester failed: Semester with name '{request.Name}' already exists for another semester.");
                    throw new ConflictException($"Семестр с названием '{request.Name}' уже существует.");
                }
                semesterToUpdate.Name = request.Name;
            }
            // ДОБАВЛЕНО: Проверка на дубликат кода
            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != semesterToUpdate.Code)
            {
                if (await _context.Semesters.AnyAsync(s => s.Code == request.Code && s.SemesterId != semesterId))
                {
                    _logger.LogWarning($"Update semester failed: Semester with code '{request.Code}' already exists for another semester.");
                    throw new ConflictException($"Семестр с кодом '{request.Code}' уже существует.");
                }
                semesterToUpdate.Code = request.Code;
            }
            // ОБНОВЛЕНО: isActive
            semesterToUpdate.IsActive = request.IsActive; // Обновляем isActive напрямую

            if (request.StartDate.HasValue)
            {
                semesterToUpdate.StartDate = request.StartDate.Value;
            }
            if (request.EndDate.HasValue)
            {
                semesterToUpdate.EndDate = request.EndDate.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict during semester update for SemesterId: {SemesterId}", semesterId);
                if (!_context.Semesters.Any(e => e.SemesterId == semesterId))
                {
                    throw new NotFoundException($"Семестр с ID {semesterId} не найден (возможно, был удален другим пользователем).");
                }
                throw; // Перевыбрасываем для обработки выше, если это истинный конфликт параллелизма
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during update of semester {semesterId}. Details: {ex.Message}");
                throw new Exception($"Произошла неожиданная ошибка при обновлении семестра с ID {semesterId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteSemesterAsync(int semesterId)
        {
            _logger.LogInformation($"Attempting to delete semester with ID: {semesterId}");

            var semesterToDelete = await _context.Semesters
                .Include(s => s.TeacherSubjectGroupAssignments)
                .FirstOrDefaultAsync(s => s.SemesterId == semesterId);

            if (semesterToDelete == null)
            {
                _logger.LogWarning($"Delete semester failed: Semester with ID {semesterId} not found.");
                throw new NotFoundException($"Семестр с ID {semesterId} не найден.");
            }

            if (semesterToDelete.TeacherSubjectGroupAssignments != null && semesterToDelete.TeacherSubjectGroupAssignments.Any())
            {
                _logger.LogWarning($"Cannot delete semester {semesterId}: {semesterToDelete.TeacherSubjectGroupAssignments.Count} associated teacher-subject-group assignments exist.");
                throw new ConflictException($"Невозможно удалить семестр с ID {semesterId}: с ним связано {semesterToDelete.TeacherSubjectGroupAssignments.Count} назначений преподавателей на предметы и группы.");
            }

            _context.Semesters.Remove(semesterToDelete);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Semester {semesterId} deleted successfully.");
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DbUpdateException occurred while deleting semester {semesterId}. Unhandled dependencies still exist. Details: {ex.InnerException?.Message ?? ex.Message}");
                throw new ConflictException($"Невозможно удалить семестр с ID {semesterId} из-за существующих связанных данных. Ошибка базы данных: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during deletion of semester {semesterId}. Details: {ex.Message}");
                throw new Exception($"Произошла неожиданная ошибка при удалении семестра с ID {semesterId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> CanUserManageSemestersAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            return user?.Role?.Name == UserRoles.Administrator;
        }

        public async Task<bool> CanUserViewAllSemestersAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            return user?.Role?.Name == UserRoles.Administrator || user?.Role?.Name == UserRoles.Teacher || user?.Role?.Name == UserRoles.Student;
        }

        public async Task<bool> CanUserViewSemesterDetailsAsync(int userId, int semesterId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            var semester = await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.SemesterId == semesterId);
            if (semester == null) return false;

            return user.Role?.Name == UserRoles.Administrator || user.Role?.Name == UserRoles.Teacher || user.Role?.Name == UserRoles.Student;
        }
    }
}
