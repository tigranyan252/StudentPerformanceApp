// Path: StudentPerformance.Api/Services/GradeService.cs

using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using StudentPerformance.Api.Models.Requests;
using StudentPerformance.Api.Services.Interfaces;
using StudentPerformance.Api.Utilities; // Для доступа к UserRoles

namespace StudentPerformance.Api.Services
{
    /// <summary>
    /// Handles CRUD operations and retrieval of grades, with integrated authorization checks.
    /// </summary>
    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public GradeService(ApplicationDbContext context, IMapper mapper, IUserService userService)
        {
            _context = context;
            _mapper = mapper;
            _userService = userService;
        }

        /// <summary>
        /// Retrieves a list of all grades, with optional filtering by student, teacher, subject, or semester.
        /// Includes related entities for complete DTO mapping.
        /// </summary>
        /// <param name="studentId">Optional: Filter by StudentId.</param>
        /// <param name="teacherId">Optional: Filter by TeacherId (from TSGA).</param>
        /// <param name="subjectId">Optional: Filter by SubjectId (from TSGA).</param>
        /// <param name="semesterId">Optional: Filter by SemesterId (from TSGA).</param>
        /// <returns>An IEnumerable of GradeDto objects.</returns>
        public async Task<IEnumerable<GradeDto>> GetAllGradesAsync(
            int? studentId = null,
            int? teacherId = null,
            int? subjectId = null,
            int? semesterId = null)
        {
            var query = _context.Grades
                // ОЧЕНЬ ВАЖНО: Включаем все необходимые связанные сущности для маппинга GradeDto
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Student)
                    .ThenInclude(s => s.Group) // Включено, если нужно для будущих нужд DTO или логики
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Teacher)
                        .ThenInclude(t => t.User)
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Subject)
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Semester)
                .Include(g => g.Assignment) // Включаем Assignment для AssignmentTitle
                .AsQueryable();

            // Apply filters if provided
            if (studentId.HasValue && studentId.Value > 0)
            {
                query = query.Where(g => g.StudentId == studentId.Value);
            }
            if (teacherId.HasValue && teacherId.Value > 0)
            {
                // Фильтруем по TeacherId из TeacherSubjectGroupAssignment, так как это источник прав на оценку
                query = query.Where(g => g.TeacherSubjectGroupAssignment != null && g.TeacherSubjectGroupAssignment.TeacherId == teacherId.Value);
            }
            if (subjectId.HasValue && subjectId.Value > 0)
            {
                // Фильтруем по SubjectId из TeacherSubjectGroupAssignment
                query = query.Where(g => g.TeacherSubjectGroupAssignment != null && g.TeacherSubjectGroupAssignment.SubjectId == subjectId.Value);
            }
            if (semesterId.HasValue && semesterId.Value > 0)
            {
                // Фильтруем по SemesterId из TeacherSubjectGroupAssignment
                query = query.Where(g => g.TeacherSubjectGroupAssignment != null && g.TeacherSubjectGroupAssignment.SemesterId == semesterId.Value);
            }

            var grades = await query.ToListAsync();
            return _mapper.Map<IEnumerable<GradeDto>>(grades); // AutoMapper выполняет маппинг в DTO
        }

        /// <summary>
        /// Retrieves a specific grade by its ID.
        /// </summary>
        /// <param name="gradeId">The ID of the grade to retrieve.</param>
        /// <returns>The GradeDto if found, otherwise null.</returns>
        public async Task<GradeDto?> GetGradeByIdAsync(int gradeId)
        {
            var grade = await _context.Grades
                .Include(g => g.Student).ThenInclude(s => s.User)
                .Include(g => g.Student).ThenInclude(s => s.Group)
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Teacher).ThenInclude(t => t.User)
                .Include(g => g.TeacherSubjectGroupAssignment).ThenInclude(tsga => tsga.Subject)
                .Include(g => g.TeacherSubjectGroupAssignment).ThenInclude(tsga => tsga.Semester)
                .Include(g => g.Assignment) // Включаем Assignment для AssignmentTitle
                .FirstOrDefaultAsync(g => g.GradeId == gradeId);

            return grade != null ? _mapper.Map<GradeDto>(grade) : null; // AutoMapper выполняет маппинг
        }

        /// <summary>
        /// Adds a new grade to the database. Performs authorization check via UserService
        /// using TeacherSubjectGroupAssignment context.
        /// </summary>
        /// <param name="request">The data for the new grade.</param>
        /// <param name="currentUserId">The ID of the user attempting to add the grade.</param>
        /// <returns>The newly created GradeDto.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not authorized.</exception>
        /// <exception cref="ArgumentException">Thrown if related entities (TSGA, student, or consistency checks fail).</exception>
        public async Task<GradeDto> AddGradeAsync(AddGradeRequest request, int currentUserId)
        {
            // 1. Authorization check: Can the current user add *any* grade?
            if (!await _userService.CanUserAddGradeAsync(currentUserId))
            {
                throw new UnauthorizedAccessException("Current user is not authorized to add grades.");
            }

            // 2. Validate existence and retrieve TeacherSubjectGroupAssignment with all necessary includes
            var tsga = await _context.TeacherSubjectGroupAssignments
                                     .Include(t => t.Teacher)
                                         .ThenInclude(t => t.User)
                                     .Include(t => t.Subject)
                                     .Include(t => t.Semester)
                                     .Include(t => t.Group)
                                     .FirstOrDefaultAsync(t => t.TeacherSubjectGroupAssignmentId == request.TeacherSubjectGroupAssignmentId);

            if (tsga == null)
            {
                throw new ArgumentException($"TeacherSubjectGroupAssignment with ID {request.TeacherSubjectGroupAssignmentId} not found.");
            }

            // 3. Authorization check (specific to TSGA): Is the current user assigned to this TSGA?
            var performingUserRole = await _userService.GetUserRoleAsync(currentUserId);
            if (performingUserRole == UserRoles.Teacher)
            {
                var teacherProfile = await _userService.GetTeacherByIdAsync(currentUserId);
                if (teacherProfile == null || tsga.TeacherId != teacherProfile.TeacherId)
                {
                    throw new UnauthorizedAccessException("Teachers can only add grades for their own assigned courses.");
                }
            }
            else if (performingUserRole != UserRoles.Administrator) // Only Admin and Teacher can add grades
            {
                throw new UnauthorizedAccessException("Only Administrators and Teachers are authorized to add grades.");
            }

            // 4. Validate Student exists and belongs to the correct group for this TSGA
            var student = await _context.Students
                                         .Include(s => s.Group)
                                         .Include(s => s.User)
                                         .FirstOrDefaultAsync(s => s.StudentId == request.StudentId);
            if (student == null)
            {
                throw new ArgumentException($"Student with ID {request.StudentId} not found.");
            }
            if (student.GroupId != tsga.GroupId)
            {
                throw new ArgumentException($"Student with ID {request.StudentId} does not belong to the group ({tsga.Group?.Name ?? "N/A"}, ID: {tsga.GroupId}) associated with the provided TeacherSubjectGroupAssignment.");
            }

            // Проверяем, существует ли задание, если оно указано
            if (request.AssignmentId.HasValue)
            {
                var assignment = await _context.Assignments.FindAsync(request.AssignmentId.Value);
                if (assignment == null)
                {
                    throw new ArgumentException("Assignment with provided ID does not exist.");
                }
            }

            // 5. Map the DTO to a Grade entity
            var newGrade = _mapper.Map<Grade>(request);

            // Set properties derived from TSGA and audit fields
            newGrade.TeacherId = tsga.TeacherId;
            newGrade.SubjectId = tsga.SubjectId;
            newGrade.SemesterId = tsga.SemesterId;
            newGrade.CreatedAt = DateTime.UtcNow;
            newGrade.UpdatedAt = DateTime.UtcNow;
            // ИСПРАВЛЕНО: Теперь AssignmentId берется из запроса
            // newGrade.AssignmentId = null; // Эту строку удаляем/комментируем
            newGrade.AssignmentId = request.AssignmentId;

            _context.Grades.Add(newGrade);
            await _context.SaveChangesAsync();

            // 7. Retrieve the newly added grade with all its related entities for proper DTO mapping
            var addedGrade = await GetGradeByIdAsync(newGrade.GradeId); // Используем наш же GetGradeByIdAsync для полной загрузки
            return addedGrade!; // Non-nullable assertion after successful retrieval
        }

        /// <summary>
        /// Updates an existing grade. Performs authorization check via UserService.
        /// IMPORTANT: This method now only updates the core grade details (value, control type, date received, status, notes).
        /// It does NOT allow changing StudentId, TeacherSubjectGroupAssignmentId, or AssignmentId via this request,
        /// as these fields are not present in UpdateGradeRequest.
        /// </summary>
        /// <param name="gradeId">The ID of the grade to update.</param>
        /// <param name="request">The updated grade data.</param>
        /// <param name="currentUserId">The ID of the user attempting to update the grade.</param>
        /// <returns>True if the grade was successfully updated, false if the grade was not found.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not authorized.</exception>
        /// <exception cref="ArgumentException">Thrown if related entities (TSGA, student, or consistency checks fail).</exception>
        public async Task<bool> UpdateGradeAsync(int gradeId, UpdateGradeRequest request, int currentUserId)
        {
            // 1. Find the existing grade with necessary includes for authorization
            var existingGrade = await _context.Grades
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Teacher) // Needed for teacher authorization check
                .FirstOrDefaultAsync(g => g.GradeId == gradeId);

            if (existingGrade == null) return false;

            // 2. Authorization check: Can the current user update *this specific* grade?
            var performingUserRole = await _userService.GetUserRoleAsync(currentUserId);

            if (performingUserRole == UserRoles.Administrator)
            {
                // Administrator can update any grade
            }
            else if (performingUserRole == UserRoles.Teacher)
            {
                // Teachers can only update grades for their own assigned courses
                var teacherProfile = await _userService.GetTeacherByIdAsync(currentUserId);
                if (teacherProfile == null || existingGrade.TeacherSubjectGroupAssignment?.TeacherId != teacherProfile.TeacherId)
                {
                    throw new UnauthorizedAccessException("Teachers can only update grades for their own assigned courses.");
                }
            }
            else
            {
                throw new UnauthorizedAccessException("You are not authorized to update grades.");
            }

            // Проверяем, существует ли задание, если оно изменено и указано в запросе
            if (request.AssignmentId.HasValue && request.AssignmentId != existingGrade.AssignmentId)
            {
                var assignment = await _context.Assignments.FindAsync(request.AssignmentId.Value);
                if (assignment == null)
                {
                    throw new ArgumentException("Assignment with provided ID does not exist.");
                }
            }


            // 3. Map updated fields from DTO to the existing entity.
            // ForAllMembers in MappingProfile ensures only non-null request fields are mapped.
            _mapper.Map(request, existingGrade);
            existingGrade.UpdatedAt = DateTime.UtcNow;

            // Update specific properties if UpdateGradeRequest does not map them or for fine-grained control
            existingGrade.AssignmentId = request.AssignmentId; // Устанавливаем AssignmentId из запроса

            // 4. Save the changes to the database.
            var affectedRows = await _context.SaveChangesAsync();
            return affectedRows > 0;
        }

        /// <summary>
        /// Deletes an existing grade. Performs authorization check via UserService.
        /// </summary>
        /// <param name="gradeId">The ID of the grade to delete.</param>
        /// <param name="currentUserId">The ID of the user attempting to delete the grade.</param>
        /// <returns>True if the grade was successfully deleted, false if the grade was not found.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not authorized.</exception>
        /// <exception cref="InvalidOperationException">Thrown if grade cannot be deleted due to dependencies.</exception>
        public async Task<bool> DeleteGradeAsync(int gradeId, int currentUserId)
        {
            // 1. Find the grade to remove with necessary includes for authorization
            var gradeToRemove = await _context.Grades
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Teacher) // Needed for teacher authorization check
                .FirstOrDefaultAsync(g => g.GradeId == gradeId);

            if (gradeToRemove == null) return false;

            // 2. Authorization check: Can the current user delete *this specific* grade?
            var isAuthorized = await _userService.CanUserDeleteGradeAsync(currentUserId, gradeToRemove.GradeId);
            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("Current user is not authorized to delete this grade.");
            }

            // 3. Remove the grade from the context and save changes
            try
            {
                _context.Grades.Remove(gradeToRemove);
                var affectedRows = await _context.SaveChangesAsync();
                return affectedRows > 0;
            }
            catch (DbUpdateException ex)
            {
                // This typically happens due to foreign key constraints if dependent entities exist
                throw new InvalidOperationException($"Cannot delete grade with ID {gradeId} because it has associated records or other dependencies. " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors
                throw new InvalidOperationException($"An unexpected error occurred while deleting grade with ID {gradeId}. " + ex.Message, ex);
            }
        }
    }
}
