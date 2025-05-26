// Path: StudentPerformance.Api/Services/SubjectService.cs

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
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

        public async Task<List<SubjectDto>> GetAllSubjectsAsync()
        {
            var subjects = await _context.Subjects.ToListAsync();
            return _mapper.Map<List<SubjectDto>>(subjects);
        }

        public async Task<SubjectDto?> GetSubjectByIdAsync(int subjectId)
        {
            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectId == subjectId);
            return _mapper.Map<SubjectDto>(subject);
        }

        public async Task<SubjectDto?> AddSubjectAsync(AddSubjectRequest request)
        {
            // Basic validation: ensure subject name is unique
            // FIX: Use request.Name instead of request.SubjectName
            if (await _context.Subjects.AnyAsync(s => s.Name == request.Name))
            {
                return null; // Or throw a specific exception for duplicate name
            }

            var subject = _mapper.Map<Subject>(request);
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            return _mapper.Map<SubjectDto>(subject);
        }

        public async Task<bool> UpdateSubjectAsync(int subjectId, UpdateSubjectRequest request)
        {
            var subjectToUpdate = await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectId == subjectId);
            if (subjectToUpdate == null) return false;

            // Basic validation: ensure updated subject name is unique, unless it's the current subject's name
            // FIX: Use request.Name instead of request.SubjectName and subjectToUpdate.Name
            if (await _context.Subjects.AnyAsync(s => s.Name == request.Name && s.SubjectId != subjectId))
            {
                return false; // Duplicate name
            }

            // Automapper will handle mapping the 'Name' property from request to subjectToUpdate
            _mapper.Map(request, subjectToUpdate);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSubjectAsync(int subjectId)
        {
            var subjectToDelete = await _context.Subjects.FirstOrDefaultAsync(s => s.SubjectId == subjectId);
            if (subjectToDelete == null) return false;

            _context.Subjects.Remove(subjectToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Authorization/Permission Checks for Subjects ---

        public async Task<bool> CanUserViewAllSubjectsAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return user.Role?.Name == "Администратор" || user.Role?.Name == "Преподаватель" || user.Role?.Name == "Студент";
        }

        public async Task<bool> CanUserViewSubjectDetailsAsync(int userId, int subjectId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            if (!await _context.Subjects.AnyAsync(s => s.SubjectId == subjectId))
            {
                return false;
            }

            return user.Role?.Name == "Администратор" || user.Role?.Name == "Преподаватель" || user.Role?.Name == "Студент";
        }

        public async Task<bool> CanUserAddSubjectAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return user.Role?.Name == "Администратор";
        }

        public async Task<bool> CanUserUpdateSubjectAsync(int userId, int subjectId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            if (!await _context.Subjects.AnyAsync(s => s.SubjectId == subjectId))
            {
                return false;
            }

            return user.Role?.Name == "Администратор";
        }

        public async Task<bool> CanUserDeleteSubjectAsync(int userId, int subjectId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            if (!await _context.Subjects.AnyAsync(s => s.SubjectId == subjectId))
            {
                return false;
            }

            return user.Role?.Name == "Администратор";
        }
    }
}