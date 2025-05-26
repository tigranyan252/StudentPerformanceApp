// Path: StudentPerformance.Api/Services/GradeService.cs

using StudentPerformance.Api.Data; // Для ApplicationDbContext
using StudentPerformance.Api.Data.Entities; // Для Grade, Student, Teacher, Subject, Semester, User
using StudentPerformance.Api.Models.DTOs; // Для GradeDto, AddGradeRequest, UpdateGradeRequest
using Microsoft.EntityFrameworkCore; // Для EF Core extension methods (Include, FirstOrDefaultAsync, AnyAsync, ToListAsync etc.)
using AutoMapper; // Для IMapper
using System.Collections.Generic; // Для List<T>, IEnumerable<T>
using System.Linq; // Для LINQ methods (Where, Select etc.)
using System.Threading.Tasks; // Для Task
using System; // Для Guid, InvalidOperationException, ArgumentException, Nullable<T> (T?), UnauthorizedAccessException

namespace StudentPerformance.Api.Services
{
    /// <summary>
    /// Handles CRUD operations and retrieval of grades, with integrated authorization checks.
    /// </summary>
    public class GradeService : IGradeService // <--- ADDED IGradeService implementation
    {
        private readonly ApplicationDbContext _context; // The database context
        private readonly IMapper _mapper; // AutoMapper instance for DTO-entity mapping
        private readonly IUserService _userService; // <--- Changed to IUserService for loose coupling

        public GradeService(ApplicationDbContext context, IMapper mapper, IUserService userService) // <--- Changed parameter to IUserService
        {
            _context = context;
            _mapper = mapper;
            _userService = userService; // Inject and initialize UserService
        }

        /// <summary>
        /// Retrieves a list of all grades, with optional filtering by student, teacher, subject, or semester.
        /// Includes related entities for complete DTO mapping.
        /// </summary>
        /// <param name="studentId">Optional: Filter by StudentId.</param>
        /// <param name="teacherId">Optional: Filter by TeacherId.</param>
        /// <param name="param name="subjectId">Optional: Filter by SubjectId.</param>
        /// <param name="semesterId">Optional: Filter by SemesterId.</param>
        /// <returns>An IEnumerable of GradeDto objects.</returns>
        public async Task<IEnumerable<GradeDto>> GetAllGradesAsync( // <--- Changed return type to IEnumerable<GradeDto>
            int? studentId = null,
            int? teacherId = null,
            int? subjectId = null,
            int? semesterId = null)
        {
            // Build the query to include all necessary related entities
            var query = _context.Grades
                                 .Include(g => g.Student).ThenInclude(s => s.User)
                                 .Include(g => g.Student).ThenInclude(s => s.Group) // Assuming Group is part of Student
                                 .Include(g => g.Teacher).ThenInclude(t => t.User)
                                 .Include(g => g.Subject)
                                 .Include(g => g.Semester)
                                 .AsQueryable(); // Start as IQueryable to apply filters dynamically

            // Apply filters if provided
            if (studentId.HasValue && studentId.Value > 0)
            {
                query = query.Where(g => g.StudentId == studentId.Value);
            }
            if (teacherId.HasValue && teacherId.Value > 0)
            {
                query = query.Where(g => g.TeacherId == teacherId.Value);
            }
            if (subjectId.HasValue && subjectId.Value > 0)
            {
                query = query.Where(g => g.SubjectId == subjectId.Value);
            }
            if (semesterId.HasValue && semesterId.Value > 0)
            {
                query = query.Where(g => g.SemesterId == semesterId.Value);
            }

            // Execute the query and map the results to DTOs
            var grades = await query.ToListAsync();
            return _mapper.Map<IEnumerable<GradeDto>>(grades); // <--- Mapped to IEnumerable
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
                                      .Include(g => g.Teacher).ThenInclude(t => t.User)
                                      .Include(g => g.Subject)
                                      .Include(g => g.Semester)
                                      .FirstOrDefaultAsync(g => g.GradeId == gradeId);

            // Map the entity to DTO if found, otherwise return null
            return grade != null ? _mapper.Map<GradeDto>(grade) : null;
        }

        /// <summary>
        /// Adds a new grade to the database. Performs authorization check via UserService.
        /// </summary>
        /// <param name="request">The data for the new grade.</param>
        /// <param name="currentUserId">The ID of the user attempting to add the grade.</param>
        /// <returns>The newly created GradeDto.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not authorized.</exception>
        /// <exception cref="ArgumentException">Thrown if related entities (student, subject, semester) are not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the teacher profile for the current user is not found.</exception>
        public async Task<GradeDto> AddGradeAsync(AddGradeRequest request, int currentUserId) // <--- Changed return type to GradeDto (non-nullable)
        {
            // 1. Authorization check: Only a teacher authorized to assign grades for this student, subject, and semester can add a grade.
            // This method `CanTeacherAssignGrade` needs to be implemented in your UserService.
            var isAuthorized = await _userService.CanTeacherAssignGrade(currentUserId, request.StudentId, request.SubjectId, request.SemesterId);
            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("Current user is not authorized to add a grade for this student, subject, and semester.");
            }

            // 2. Validate existence of related entities to ensure data integrity.
            var student = await _context.Students.FindAsync(request.StudentId);
            if (student == null) throw new ArgumentException($"Student with ID {request.StudentId} not found.");

            var subject = await _context.Subjects.FindAsync(request.SubjectId);
            if (subject == null) throw new ArgumentException($"Subject with ID {request.SubjectId} not found.");

            var semester = await _context.Semesters.FindAsync(request.SemesterId);
            if (semester == null) throw new ArgumentException($"Semester with ID {request.SemesterId} not found.");

            // 3. Get the TeacherId associated with the current UserId
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == currentUserId);
            if (teacher == null) throw new InvalidOperationException("Teacher profile not found for the current user. Cannot assign a grade without a teacher profile.");

            // 4. Map the DTO to a Grade entity and set the TeacherId
            var newGrade = _mapper.Map<Grade>(request);
            newGrade.TeacherId = teacher.TeacherId; // Assign the actual TeacherId

            // 5. Add the new grade to the context and save changes
            _context.Grades.Add(newGrade);
            await _context.SaveChangesAsync();

            // 6. Retrieve the newly added grade with all its related entities for proper DTO mapping
            var addedGrade = await _context.Grades
                                           .Include(g => g.Student).ThenInclude(s => s.User)
                                           .Include(g => g.Student).ThenInclude(s => s.Group)
                                           .Include(g => g.Teacher).ThenInclude(t => t.User)
                                           .Include(g => g.Subject)
                                           .Include(g => g.Semester)
                                           .FirstOrDefaultAsync(g => g.GradeId == newGrade.GradeId);

            // Return the mapped DTO. If addedGrade is null, it indicates an issue after saving.
            // Using null-forgiving operator assuming it will not be null after a successful save.
            return _mapper.Map<GradeDto>(addedGrade!);
        }

        /// <summary>
        /// Updates an existing grade. Performs authorization check via UserService.
        /// </summary>
        /// <param name="gradeId">The ID of the grade to update.</param>
        /// <param name="request">The updated grade data.</param>
        /// <param name="currentUserId">The ID of the user attempting to update the grade.</param>
        /// <returns>True if the grade was successfully updated, false if the grade was not found.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not authorized.</exception>
        public async Task<bool> UpdateGradeAsync(int gradeId, UpdateGradeRequest request, int currentUserId)
        {
            // 1. Authorization check: Only a teacher authorized to update this specific grade can do so.
            // This method `CanTeacherUpdateGrade` needs to be implemented in your UserService.
            var isAuthorized = await _userService.CanTeacherUpdateGrade(currentUserId, gradeId);
            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("Current user is not authorized to update this grade.");
            }

            // 2. Find the existing grade
            var existingGrade = await _context.Grades.FindAsync(gradeId);
            if (existingGrade == null) return false; // Grade not found

            // 3. Map updated fields from DTO to the existing entity. AutoMapper handles this efficiently.
            // NOTE: We are intentionally NOT checking or updating StudentId, SubjectId, SemesterId here,
            // as these are typically considered immutable identifying fields for a grade.
            // If you need to change them, it usually implies creating a new grade entry.
            _mapper.Map(request, existingGrade);

            // 4. Save the changes to the database.
            var affectedRows = await _context.SaveChangesAsync();
            return affectedRows > 0; // Return true if changes were applied
        }

        /// <summary>
        /// Deletes an existing grade. Performs authorization check via UserService.
        /// </summary>
        /// <param name="gradeId">The ID of the grade to delete.</param>
        /// <param name="currentUserId">The ID of the user attempting to delete the grade.</param>
        /// <returns>True if the grade was successfully deleted, false if the grade was not found.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current user is not authorized.</exception>
        public async Task<bool> DeleteGradeAsync(int gradeId, int currentUserId)
        {
            // 1. Authorization check: Only a teacher authorized to delete this specific grade can do so.
            // This method `CanTeacherDeleteGrade` needs to be implemented in your UserService.
            var isAuthorized = await _userService.CanTeacherDeleteGrade(currentUserId, gradeId);
            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("Current user is not authorized to delete this grade.");
            }

            // 2. Find the grade to remove
            var gradeToRemove = await _context.Grades.FindAsync(gradeId);
            if (gradeToRemove == null) return false; // Grade not found

            // 3. Remove the grade from the context and save changes
            _context.Grades.Remove(gradeToRemove);
            var affectedRows = await _context.SaveChangesAsync();
            return affectedRows > 0; // Return true if deletion was successful
        }
    }
}