// Path: StudentPerformance.Api/Services/Interfaces/IAssignmentService.cs

using StudentPerformance.Api.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.Requests; // Если понадобятся запросы для Add/Update

namespace StudentPerformance.Api.Services.Interfaces
{
    // НОВОЕ: Интерфейс для работы с общими заданиями (Homework, Projects)
    public interface IAssignmentService
    {
        Task<IEnumerable<AssignmentDto>> GetAllAssignmentsAsync(); // Возвращает общие задания
        Task<AssignmentDto?> GetAssignmentByIdAsync(int assignmentId);
        // Добавьте другие методы, если нужны: Add, Update, Delete
        Task<AssignmentDto?> AddAssignmentAsync(AddAssignmentRequest request);
        Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateAssignmentRequest request);
        Task<bool> DeleteAssignmentAsync(int assignmentId);
    }
}
