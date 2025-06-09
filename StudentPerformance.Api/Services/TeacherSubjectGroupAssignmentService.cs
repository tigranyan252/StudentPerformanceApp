// Path: StudentPerformance.Api/Services/TeacherSubjectGroupAssignmentService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // ДОБАВЛЕНО: для ILogger
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Exceptions; // ДОБАВЛЕНО: для NotFoundException, ConflictException
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services
{
    public class TeacherSubjectGroupAssignmentService : ITeacherSubjectGroupAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TeacherSubjectGroupAssignmentService> _logger; // ДОБАВЛЕНО: поле для логирования

        public TeacherSubjectGroupAssignmentService(ApplicationDbContext context, IMapper mapper, ILogger<TeacherSubjectGroupAssignmentService> logger) // ИСПРАВЛЕНО: Обновлен конструктор
        {
            _context = context;
            _mapper = mapper;
            _logger = logger; // Инициализация логгера
        }

        public async Task<TeacherSubjectGroupAssignmentDto> AddAssignmentAsync(AddTeacherSubjectGroupAssignmentRequest request)
        {
            _logger.LogInformation("Attempting to add new assignment for TeacherId: {TeacherId}, SubjectId: {SubjectId}, GroupId: {GroupId}, SemesterId: {SemesterId}",
                request.TeacherId, request.SubjectId, request.GroupId, request.SemesterId);

            try
            {
                var existingAssignment = await _context.TeacherSubjectGroupAssignments
                    .AnyAsync(a => a.TeacherId == request.TeacherId &&
                                   a.SubjectId == request.SubjectId &&
                                   a.GroupId == request.GroupId &&
                                   a.SemesterId == request.SemesterId);

                if (existingAssignment)
                {
                    _logger.LogWarning("Add assignment failed: Identical assignment already exists for TeacherId: {TeacherId}, SubjectId: {SubjectId}, GroupId: {GroupId}, SemesterId: {SemesterId}",
                        request.TeacherId, request.SubjectId, request.GroupId, request.SemesterId);
                    throw new ConflictException("An identical assignment already exists.");
                }

                // Проверка существования связанных сущностей перед созданием назначения
                var teacherExists = await _context.Teachers.AnyAsync(t => t.TeacherId == request.TeacherId);
                var subjectExists = await _context.Subjects.AnyAsync(s => s.SubjectId == request.SubjectId);
                var groupExists = request.GroupId.HasValue ? await _context.Groups.AnyAsync(g => g.GroupId == request.GroupId.Value) : true; // Группа может быть null
                var semesterExists = await _context.Semesters.AnyAsync(s => s.SemesterId == request.SemesterId);

                if (!teacherExists) throw new NotFoundException($"Teacher with ID {request.TeacherId} not found.");
                if (!subjectExists) throw new NotFoundException($"Subject with ID {request.SubjectId} not found.");
                if (request.GroupId.HasValue && !groupExists) throw new NotFoundException($"Group with ID {request.GroupId.Value} not found.");
                if (!semesterExists) throw new NotFoundException($"Semester with ID {request.SemesterId} not found.");


                var assignment = _mapper.Map<TeacherSubjectGroupAssignment>(request);
                assignment.CreatedAt = DateTime.UtcNow; // Установка метки времени создания
                assignment.UpdatedAt = DateTime.UtcNow; // Установка метки времени обновления

                _context.TeacherSubjectGroupAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // Загружаем навигационные свойства для DTO, чтобы вернуть полную информацию
                await _context.Entry(assignment)
                    .Reference(a => a.Teacher).LoadAsync();
                if (assignment.Teacher != null)
                {
                    await _context.Entry(assignment.Teacher)
                        .Reference(t => t.User).LoadAsync();
                }
                await _context.Entry(assignment).Reference(a => a.Subject).LoadAsync();
                await _context.Entry(assignment).Reference(a => a.Group).LoadAsync();
                await _context.Entry(assignment).Reference(a => a.Semester).LoadAsync();

                _logger.LogInformation("Assignment {AssignmentId} added successfully.", assignment.TeacherSubjectGroupAssignmentId);
                return _mapper.Map<TeacherSubjectGroupAssignmentDto>(assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding assignment for TeacherId: {TeacherId}, SubjectId: {SubjectId}, GroupId: {GroupId}, SemesterId: {SemesterId}",
                    request.TeacherId, request.SubjectId, request.GroupId, request.SemesterId);
                throw; // Перебрасываем исключение, чтобы контроллер мог его обработать
            }
        }

        public async Task<TeacherSubjectGroupAssignmentDto?> GetAssignmentByIdAsync(int assignmentId)
        {
            _logger.LogInformation("Attempting to retrieve assignment with ID: {AssignmentId}", assignmentId);
            var assignment = await _context.TeacherSubjectGroupAssignments
                .Include(a => a.Teacher)
                    .ThenInclude(t => t.User) // Загружаем User для Teacher для FullName
                .Include(a => a.Subject)
                .Include(a => a.Group)
                .Include(a => a.Semester)
                .FirstOrDefaultAsync(a => a.TeacherSubjectGroupAssignmentId == assignmentId);

            if (assignment == null)
            {
                _logger.LogWarning("Assignment with ID {AssignmentId} not found.", assignmentId);
            }
            else
            {
                _logger.LogInformation("Assignment {AssignmentId} retrieved successfully.", assignmentId);
            }
            return _mapper.Map<TeacherSubjectGroupAssignmentDto>(assignment);
        }

        public async Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAllAssignmentsAsync()
        {
            _logger.LogInformation("Attempting to retrieve all assignments.");
            var assignments = await _context.TeacherSubjectGroupAssignments
                .Include(a => a.Teacher)
                    .ThenInclude(t => t.User) // Загружаем User для Teacher для FullName
                .Include(a => a.Subject)
                .Include(a => a.Group)
                .Include(a => a.Semester)
                .ToListAsync();

            _logger.LogInformation("{Count} assignments retrieved.", assignments.Count);
            return _mapper.Map<IEnumerable<TeacherSubjectGroupAssignmentDto>>(assignments);
        }

        public async Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateTeacherSubjectGroupAssignmentRequest request)
        {
            _logger.LogInformation("Attempting to update assignment with ID: {AssignmentId}", assignmentId);

            var assignmentToUpdate = await _context.TeacherSubjectGroupAssignments.FindAsync(assignmentId);

            if (assignmentToUpdate == null)
            {
                _logger.LogWarning("Update assignment failed: Assignment with ID {AssignmentId} not found.", assignmentId);
                throw new NotFoundException($"Assignment with ID {assignmentId} not found.");
            }

            var duplicateCheck = await _context.TeacherSubjectGroupAssignments
                .AnyAsync(a => a.TeacherId == request.TeacherId &&
                               a.SubjectId == request.SubjectId &&
                               a.GroupId == request.GroupId &&
                               a.SemesterId == request.SemesterId &&
                               a.TeacherSubjectGroupAssignmentId != assignmentId);

            if (duplicateCheck)
            {
                _logger.LogWarning("Update assignment failed: Would create a duplicate assignment for ID {AssignmentId}.", assignmentId);
                throw new ConflictException("Update would create a duplicate assignment.");
            }

            // Проверка существования связанных сущностей при обновлении
            var teacherExists = await _context.Teachers.AnyAsync(t => t.TeacherId == request.TeacherId);
            var subjectExists = await _context.Subjects.AnyAsync(s => s.SubjectId == request.SubjectId);
            var groupExists = request.GroupId.HasValue ? await _context.Groups.AnyAsync(g => g.GroupId == request.GroupId.Value) : true;
            var semesterExists = await _context.Semesters.AnyAsync(s => s.SemesterId == request.SemesterId);

            if (!teacherExists) throw new NotFoundException($"Teacher with ID {request.TeacherId} not found.");
            if (!subjectExists) throw new NotFoundException($"Subject with ID {request.SubjectId} not found.");
            if (request.GroupId.HasValue && !groupExists) throw new NotFoundException($"Group with ID {request.GroupId.Value} not found.");
            if (!semesterExists) throw new NotFoundException($"Semester with ID {request.SemesterId} not found.");


            _mapper.Map(request, assignmentToUpdate);
            assignmentToUpdate.UpdatedAt = DateTime.UtcNow; // Установка метки времени обновления

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Assignment {AssignmentId} updated successfully.", assignmentId);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict occurred while updating assignment ID {AssignmentId}. Checking if it still exists...", assignmentId);
                if (!await _context.TeacherSubjectGroupAssignments.AnyAsync(e => e.TeacherSubjectGroupAssignmentId == assignmentId))
                {
                    _logger.LogWarning("Assignment {AssignmentId} was deleted by another user during a concurrency conflict.", assignmentId);
                    throw new NotFoundException($"Assignment with ID {assignmentId} was deleted by another user.");
                }
                _logger.LogError(ex, "Concurrency conflict occurred while updating assignment ID {AssignmentId}, but assignment still exists.", assignmentId);
                throw new ConflictException($"Concurrency conflict when updating assignment with ID {assignmentId}. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating assignment with ID {AssignmentId}.", assignmentId);
                throw; // Перебрасываем исключение, чтобы контроллер мог его обработать
            }
        }

        public async Task<bool> DeleteAssignmentAsync(int assignmentId)
        {
            _logger.LogInformation("Attempting to delete assignment with ID: {AssignmentId}", assignmentId);

            var assignment = await _context.TeacherSubjectGroupAssignments
                                           .Include(tsga => tsga.Grades) // Включаем связанные оценки
                                           .FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == assignmentId);

            if (assignment == null)
            {
                _logger.LogWarning("Delete assignment failed: Assignment with ID {AssignmentId} not found.", assignmentId);
                throw new NotFoundException($"Assignment with ID {assignmentId} not found.");
            }

            // Проверка на связанные оценки
            if (assignment.Grades != null && assignment.Grades.Any())
            {
                _logger.LogWarning("Cannot delete assignment {AssignmentId}: {Count} associated grades exist.", assignmentId, assignment.Grades.Count);
                throw new ConflictException($"Cannot delete assignment with ID {assignmentId} because {assignment.Grades.Count} associated grades exist.");
            }

            _context.TeacherSubjectGroupAssignments.Remove(assignment);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Assignment {AssignmentId} deleted successfully.", assignmentId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException occurred while deleting assignment ID {AssignmentId}. Unhandled dependencies still exist. Details: {Message}", assignmentId, ex.InnerException?.Message ?? ex.Message);
                throw new ConflictException($"Cannot delete assignment with ID {assignmentId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting assignment with ID {AssignmentId}.", assignmentId);
                throw; // Перебрасываем исключение, чтобы контроллер мог его обработать
            }
        }

        public async Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAssignmentsForTeacherAsync(int teacherId)
        {
            _logger.LogInformation("Attempting to retrieve assignments for Teacher ID: {TeacherId}", teacherId);
            var assignments = await _context.TeacherSubjectGroupAssignments
                .Where(a => a.TeacherId == teacherId)
                .Include(a => a.Teacher)
                    .ThenInclude(t => t.User) // Загружаем User для Teacher для FullName
                .Include(a => a.Subject)
                .Include(a => a.Group)
                .Include(a => a.Semester)
                .ToListAsync();

            _logger.LogInformation("{Count} assignments retrieved for Teacher ID {TeacherId}.", assignments.Count, teacherId);
            return _mapper.Map<IEnumerable<TeacherSubjectGroupAssignmentDto>>(assignments);
        }
    }
}
