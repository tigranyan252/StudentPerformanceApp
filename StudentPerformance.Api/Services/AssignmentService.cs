// Path: StudentPerformance.Api/Services/AssignmentService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities; // Assuming your entities are here
using StudentPerformance.Api.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System; // For ArgumentException if needed

namespace StudentPerformance.Api.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AssignmentService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<TeacherSubjectGroupAssignmentDto?> AddAssignmentAsync(AddTeacherSubjectGroupAssignmentRequest request)
        {
            // Check if such an assignment already exists to prevent duplicates
            var existingAssignment = await _context.TeacherSubjectGroupAssignments
                .AnyAsync(a => a.TeacherId == request.TeacherId &&
                               a.SubjectId == request.SubjectId &&
                               a.GroupId == request.GroupId &&
                               a.SemesterId == request.SemesterId);

            if (existingAssignment)
            {
                return null; // Assignment already exists
            }

            // Map DTO to entity
            var assignment = _mapper.Map<TeacherSubjectGroupAssignment>(request);

            _context.TeacherSubjectGroupAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Return the newly created assignment as DTO
            return _mapper.Map<TeacherSubjectGroupAssignmentDto>(assignment);
        }

        public async Task<TeacherSubjectGroupAssignmentDto?> GetAssignmentByIdAsync(int assignmentId)
        {
            var assignment = await _context.TeacherSubjectGroupAssignments
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Group)
                .Include(a => a.Semester)
                .FirstOrDefaultAsync(a => a.TeacherSubjectGroupAssignmentId == assignmentId);

            return _mapper.Map<TeacherSubjectGroupAssignmentDto>(assignment);
        }

        public async Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAllAssignmentsAsync()
        {
            var assignments = await _context.TeacherSubjectGroupAssignments
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Group)
                .Include(a => a.Semester)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TeacherSubjectGroupAssignmentDto>>(assignments);
        }

        public async Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateTeacherSubjectGroupAssignmentRequest request)
        {
            var assignment = await _context.TeacherSubjectGroupAssignments.FindAsync(assignmentId);

            if (assignment == null)
            {
                return false; // Assignment not found
            }

            // Check for potential duplicate after update (if changing teacher, subject, group, semester)
            var duplicateCheck = await _context.TeacherSubjectGroupAssignments
                .AnyAsync(a => a.TeacherId == request.TeacherId &&
                               a.SubjectId == request.SubjectId &&
                               a.GroupId == request.GroupId &&
                               a.SemesterId == request.SemesterId &&
                               a.TeacherSubjectGroupAssignmentId != assignmentId); // Exclude current assignment

            if (duplicateCheck)
            {
                return false; // Update would create a duplicate assignment
            }

            // Update properties from the request DTO
            _mapper.Map(request, assignment); // AutoMapper will update the entity

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TeacherSubjectGroupAssignments.AnyAsync(e => e.TeacherSubjectGroupAssignmentId == assignmentId))
                {
                    return false; // Assignment was deleted by another process
                }
                throw; // Re-throw other concurrency issues
            }
            catch (Exception)
            {
                // Log the exception
                return false; // General update failure
            }
        }

        public async Task<bool> DeleteAssignmentAsync(int assignmentId)
        {
            var assignment = await _context.TeacherSubjectGroupAssignments.FindAsync(assignmentId);
            if (assignment == null)
            {
                return false; // Assignment not found
            }

            _context.TeacherSubjectGroupAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}