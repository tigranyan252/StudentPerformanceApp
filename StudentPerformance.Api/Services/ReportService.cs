// StudentPerformance.Api/Services/ReportService.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Services.Interfaces;
using StudentPerformance.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, IUserService userService, ILogger<ReportService> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        public async Task<IEnumerable<StudentGradesSummaryDto>> GetStudentGradesSummaryAsync(
            int? studentId,
            int? groupId,
            int? semesterId,
            int currentUserId,
            string currentUserRole)
        {
            _logger.LogInformation("Generating student grades summary report for UserId: {CurrentUserId}, Role: {CurrentUserRole}", (object)currentUserId, (object)currentUserRole);
            _logger.LogInformation("Report filters: StudentId={StudentId}, GroupId={GroupId}, SemesterId={SemesterId}", (object)studentId, (object)groupId, (object)semesterId);

            IQueryable<Grade> query = _context.Grades
                .Include(g => g.Student)
                    .ThenInclude(s => s.User) // Важно: убедиться, что User включен для доступа к имени
                .Include(g => g.Student)
                    .ThenInclude(s => s.Group)
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Subject)
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Semester)
                .Include(g => g.TeacherSubjectGroupAssignment)
                    .ThenInclude(tsga => tsga.Teacher)
                    .ThenInclude(t => t.User);

            // --- Применение фильтров в зависимости от роли и переданных параметров ---
            if (currentUserRole == UserRoles.Student)
            {
                var studentProfile = await _userService.GetStudentByIdAsync(currentUserId);
                if (studentProfile == null)
                {
                    _logger.LogWarning("Student profile not found for UserId: {CurrentUserId}. Cannot generate report.", (object)currentUserId);
                    throw new UnauthorizedAccessException("Student profile not found. Access denied.");
                }

                query = query.Where(g => g.StudentId == studentProfile.StudentId);
                _logger.LogInformation("Report filtered by authenticated StudentId: {StudentProfileId}", (object)studentProfile.StudentId);

                if (studentId.HasValue && studentId.Value != studentProfile.StudentId)
                {
                    _logger.LogWarning("Student {CurrentStudentId} attempted to request grades for StudentId {RequestedStudentId}. Access denied.", (object)studentProfile.StudentId, (object)studentId.Value);
                    throw new UnauthorizedAccessException("You are only allowed to view your own grades report.");
                }

                if (groupId.HasValue && studentProfile.GroupId != 0 && groupId.Value != studentProfile.GroupId)
                {
                    _logger.LogWarning("Student {CurrentStudentId} attempted to request grades for GroupId {RequestedGroupId}. Access denied.", (object)studentProfile.StudentId, (object)groupId.Value);
                    throw new UnauthorizedAccessException("You are only allowed to view grades for your own group.");
                }

            }
            else if (currentUserRole == UserRoles.Teacher)
            {
                var teacherProfile = await _userService.GetTeacherByIdAsync(currentUserId);
                if (teacherProfile == null)
                {
                    _logger.LogWarning("Teacher profile not found for UserId: {CurrentUserId}. Cannot generate report.", (object)currentUserId);
                    throw new UnauthorizedAccessException("Teacher profile not found. Access denied.");
                }

                query = query.Where(g => g.TeacherSubjectGroupAssignment.TeacherId == teacherProfile.TeacherId);
                _logger.LogInformation("Report filtered by authenticated TeacherId: {TeacherProfileId}", (object)teacherProfile.TeacherId);

                if (studentId.HasValue)
                {
                    var studentEntity = await _context.Students
                        .AsNoTracking()
                        .Where(s => s.StudentId == studentId.Value)
                        .Select(s => new { s.GroupId })
                        .FirstOrDefaultAsync();

                    int? studentGroupId = studentEntity?.GroupId;

                    bool studentInTeacherGroups = false;
                    if (studentGroupId.HasValue)
                    {
                        studentInTeacherGroups = await _context.TeacherSubjectGroupAssignments
                            .AnyAsync(tsga => tsga.TeacherId == teacherProfile.TeacherId && tsga.GroupId == studentGroupId.Value);
                    }

                    if (!studentInTeacherGroups)
                    {
                        _logger.LogWarning("Teacher {TeacherId} attempted to view grades for StudentId {StudentId} not in their assigned groups. Access denied.", (object)teacherProfile.TeacherId, (object)studentId.Value);
                        throw new UnauthorizedAccessException("You are not authorized to view grades for this student.");
                    }
                    query = query.Where(g => g.StudentId == studentId.Value);
                }
                else if (groupId.HasValue)
                {
                    bool groupAssignedToTeacher = await _context.TeacherSubjectGroupAssignments
                        .AnyAsync(tsga => tsga.TeacherId == teacherProfile.TeacherId && tsga.GroupId == groupId.Value);

                    if (!groupAssignedToTeacher)
                    {
                        _logger.LogWarning("Teacher {TeacherId} attempted to view grades for GroupId {GroupId} not assigned to them. Access denied.", (object)teacherProfile.TeacherId, (object)groupId.Value);
                        throw new UnauthorizedAccessException("You are not authorized to view grades for this group.");
                    }
                    query = query.Where(g => g.Student.GroupId == groupId.Value);
                }
            }
            else if (currentUserRole == UserRoles.Administrator)
            {
                _logger.LogInformation("User is Administrator. Applying all provided filters.");
                if (studentId.HasValue)
                {
                    query = query.Where(g => g.StudentId == studentId.Value);
                }
                if (groupId.HasValue)
                {
                    query = query.Where(g => g.Student.GroupId == groupId.Value);
                }
            }
            else
            {
                _logger.LogWarning("Unauthorized role {CurrentUserRole} for report generation. UserId: {CurrentUserId}", (object)currentUserRole, (object)currentUserId);
                throw new UnauthorizedAccessException("Your role is not authorized to generate this report.");
            }

            if (semesterId.HasValue)
            {
                query = query.Where(g => g.TeacherSubjectGroupAssignment.SemesterId == semesterId.Value);
            }

            // --- Агрегация данных ---
            var reportData = await query
                .GroupBy(g => new {
                    g.StudentId,
                    g.TeacherSubjectGroupAssignment.SubjectId,
                    StudentFirstName = g.Student.User.FirstName, // Включаем имя студента
                    StudentLastName = g.Student.User.LastName
                }) // Включаем фамилию студента
                .Select(group => new StudentGradesSummaryDto
                {
                    StudentId = group.Key.StudentId,
                    StudentFirstName = group.Key.StudentFirstName, // Заполняем новое поле
                    StudentLastName = group.Key.StudentLastName,   // Заполняем новое поле
                    SubjectId = group.Key.SubjectId,
                    AverageGrade = (double)group.Average(g => g.Value),
                    GradeCount = group.Count()
                })
                .OrderBy(s => s.StudentId)
                .ThenBy(s => s.SubjectId)
                .ToListAsync();

            _logger.LogInformation("Successfully generated student grades summary report with {Count} entries.", (object)reportData.Count());
            return reportData;
        }
    }
}
