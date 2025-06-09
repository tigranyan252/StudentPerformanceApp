// Path: StudentPerformance.Api/Services/AssignmentService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data; // Ваш DbContext
using StudentPerformance.Api.Models.DTOs; // Ваши DTOs, включая AssignmentDto, SubjectDto, SemesterDto
using StudentPerformance.Api.Services.Interfaces; // Для IAssignmentService
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.Requests; // Для Add/Update requests
using System;
using StudentPerformance.Api.Data.Entities; // Для DateTime

namespace StudentPerformance.Api.Services
{
    // НОВОЕ: Сервис для работы с общими заданиями (Homework, Projects)
    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AssignmentService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>
        /// Получает список всех общих заданий, включая связанные данные по предметам и семестрам (через TeacherSubjectGroupAssignment).
        /// </summary>
        /// <returns>Список AssignmentDto.</returns>
        public async Task<IEnumerable<AssignmentDto>> GetAllAssignmentsAsync()
        {
            // Загружаем сущности Assignment с связанными TeacherSubjectGroupAssignment, Subject и Semester.
            var assignments = await _context.Assignments
                                 .Include(a => a.TeacherSubjectGroupAssignment) // Включаем связь с TSGA
                                     .ThenInclude(tsga => tsga.Subject) // Затем включаем Subject из TSGA
                                 .Include(a => a.TeacherSubjectGroupAssignment) // Снова TSGA для включения Semester (так как это отдельная ветка ThenInclude)
                                     .ThenInclude(tsga => tsga.Semester) // Затем включаем Semester из TSGA
                                 .ToListAsync(); // Получаем список сущностей Assignment

            // Явное маппирование списка сущностей Assignment на список AssignmentDto с помощью AutoMapper
            return _mapper.Map<IEnumerable<AssignmentDto>>(assignments);
        }

        /// <summary>
        /// Получает задание по ID.
        /// </summary>
        /// <param name="assignmentId">ID задания.</param>
        /// <returns>AssignmentDto или null, если не найдено.</returns>
        public async Task<AssignmentDto?> GetAssignmentByIdAsync(int assignmentId)
        {
            var assignment = await _context.Assignments
                                 .Include(a => a.TeacherSubjectGroupAssignment)
                                     .ThenInclude(tsga => tsga.Subject)
                                 .Include(a => a.TeacherSubjectGroupAssignment)
                                     .ThenInclude(tsga => tsga.Semester)
                                 .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            return _mapper.Map<AssignmentDto>(assignment);
        }

        /// <summary>
        /// Добавляет новое общее задание.
        /// </summary>
        /// <param name="request">Данные для нового задания, включая TeacherSubjectGroupAssignmentId.</param>
        /// <returns>Созданный AssignmentDto или null, если операция не удалась (например, TeacherSubjectGroupAssignmentId не найден).</returns>
        public async Task<AssignmentDto?> AddAssignmentAsync(AddAssignmentRequest request)
        {
            // Проверка, существует ли TeacherSubjectGroupAssignment
            // Загружаем TSGA, включая его Subject и Semester, чтобы они были доступны для DTO после сохранения
            var tsga = await _context.TeacherSubjectGroupAssignments
                                     .Include(t => t.Subject)
                                     .Include(t => t.Semester)
                                     .FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == request.TeacherSubjectGroupAssignmentId);
            if (tsga == null)
            {
                throw new ArgumentException($"Provided TeacherSubjectGroupAssignmentId {request.TeacherSubjectGroupAssignmentId} does not exist.");
            }

            var assignment = _mapper.Map<Assignment>(request);
            // TeacherSubjectGroupAssignmentId уже замаппится из request, так как теперь он в DTO
            assignment.CreatedAt = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            // После сохранения, привязываем загруженный TSGA к сущности assignment,
            // чтобы AutoMapper мог использовать его для маппинга в AssignmentDto.
            assignment.TeacherSubjectGroupAssignment = tsga;

            return _mapper.Map<AssignmentDto>(assignment);
        }

        /// <summary>
        /// Обновляет существующее общее задание.
        /// </summary>
        /// <param name="assignmentId">ID задания для обновления.</param>
        /// <param name="request">Данные для обновления.</param>
        /// <returns>True, если успешно обновлено, False, если задание не найдено или обновление не удалось.</returns>
        public async Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateAssignmentRequest request)
        {
            var assignment = await _context.Assignments
                // Включаем TeacherSubjectGroupAssignment, если он нужен для DTO или логики после обновления
                .Include(a => a.TeacherSubjectGroupAssignment)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (assignment == null)
            {
                return false; // Задание не найдено
            }

            // ИСПРАВЛЕНО: Если TeacherSubjectGroupAssignmentId изменяется, проверьте его существование
            if (request.TeacherSubjectGroupAssignmentId.HasValue &&
                request.TeacherSubjectGroupAssignmentId.Value != assignment.TeacherSubjectGroupAssignmentId)
            {
                // Загружаем новый TSGA с его Subject и Semester, если он изменился
                var newTsga = await _context.TeacherSubjectGroupAssignments
                                            .Include(tsga => tsga.Subject)
                                            .Include(tsga => tsga.Semester)
                                            .FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == request.TeacherSubjectGroupAssignmentId.Value);
                if (newTsga == null)
                {
                    throw new ArgumentException($"Provided TeacherSubjectGroupAssignmentId {request.TeacherSubjectGroupAssignmentId.Value} does not exist for update.");
                }
                assignment.TeacherSubjectGroupAssignmentId = request.TeacherSubjectGroupAssignmentId.Value;
                // Присваиваем объект, чтобы EF Core мог отслеживать изменения и связанные данные обновились
                assignment.TeacherSubjectGroupAssignment = newTsga;
            }

            // Применяем обновления из запроса. ForAllMembers в MappingProfile позаботится о null-полях.
            _mapper.Map(request, assignment);
            assignment.UpdatedAt = DateTime.UtcNow; // Обновляем дату изменения

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Assignments.AnyAsync(e => e.AssignmentId == assignmentId))
                {
                    return false; // Конфликт параллелизма, но запись уже удалена
                }
                throw; // Другой конфликт параллелизма, перебросить
            }
            catch (Exception ex)
            {
                // Логирование других ошибок
                throw new InvalidOperationException($"An unexpected error occurred while updating assignment with ID {assignmentId}. " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Удаляет общее задание.
        /// </summary>
        /// <param name="assignmentId">ID задания для удаления.</param>
        /// <returns>True, если успешно удалено, False, если задание не найдено.</returns>
        public async Task<bool> DeleteAssignmentAsync(int assignmentId)
        {
            var assignment = await _context.Assignments.FindAsync(assignmentId);
            if (assignment == null)
            {
                return false; // Задание не найдено
            }

            try
            {
                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Cannot delete assignment with ID {assignmentId} because it has associated grades or other dependencies. " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
