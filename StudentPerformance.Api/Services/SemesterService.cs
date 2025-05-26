// Path: StudentPerformance.Api/Services/SemesterService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Models.DTOs; // Make sure this namespace is correct for your DTOs
using StudentPerformance.Api.Data.Entities; // Assuming your Semester entity is defined here
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services // Make sure this namespace is correct
{
    public class SemesterService : ISemesterService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SemesterService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SemesterDto>> GetAllSemestersAsync()
        {
            var semesters = await _context.Semesters.ToListAsync();
            // Maps a collection of Semester entities to a collection of SemesterDto
            return _mapper.Map<IEnumerable<SemesterDto>>(semesters);
        }

        public async Task<SemesterDto?> GetSemesterByIdAsync(int semesterId)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);
            // Maps a single Semester entity to a SemesterDto, returns null if entity is null
            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task<SemesterDto?> AddSemesterAsync(AddSemesterRequest request)
        {
            // Optional: Add business logic check, e.g., prevent duplicate semester names/date overlaps
            // For example, if a semester with the same name already exists:
            if (await _context.Semesters.AnyAsync(s => s.Name == request.Name))
            {
                // You could throw a custom exception here or return null,
                // the controller would then translate this into a BadRequest.
                return null;
            }

            // Map DTO to entity
            var semester = _mapper.Map<Semester>(request);

            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();

            // Map the newly created entity back to a DTO to return
            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task<bool> UpdateSemesterAsync(int semesterId, UpdateSemesterRequest request)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);
            if (semester == null)
            {
                return false; // Semester not found
            }

            // Optional: Add business logic check, e.g., prevent updating to a duplicate name
            if (semester.Name != request.Name && await _context.Semesters.AnyAsync(s => s.Name == request.Name && s.SemesterId != semesterId))
            {
                return false; // A different semester with the new name already exists
            }

            // Map DTO properties to the existing entity
            _mapper.Map(request, semester); // This updates properties on the 'semester' object

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle concurrency conflicts if needed, e.g., if the semester was deleted by another process
                if (!await _context.Semesters.AnyAsync(e => e.SemesterId == semesterId))
                {
                    return false; // Semester was deleted by another user/process
                }
                throw; // Re-throw other concurrency exceptions
            }
            catch (System.Exception)
            {
                // Log the exception if a general error occurs during save
                return false; // Indicate update failed due to general error
            }
        }

        public async Task<bool> DeleteSemesterAsync(int semesterId)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);
            if (semester == null)
            {
                return false; // Semester not found
            }

            // Optional: Add business logic to prevent deletion if there are associated entities
            // For example, if you can't delete a semester that has grades or assignments linked to it:
            // if (await _context.Grades.AnyAsync(g => g.SemesterId == semesterId) ||
            //     await _context.Assignments.AnyAsync(a => a.SemesterId == semesterId))
            // {
            //     return false; // Cannot delete semester with existing data
            // }

            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}