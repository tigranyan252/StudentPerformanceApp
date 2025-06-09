// Path: StudentPerformance.Api/Models/DTOs/SemesterDto.cs

using System; // Required for DateTime

namespace StudentPerformance.Api.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for Semester entities.
    /// This DTO represents the basic information of an academic semester.
    /// </summary>
    public class SemesterDto
    {
        public int SemesterId { get; set; } // The unique identifier for the semester (corresponds to SemesterId in entity)

        public string Name { get; set; } = string.Empty; // The name of the semester (e.g., "Fall 2024", "Spring 2025")

        public string? Code { get; set; } // ДОБАВЛЕНО: Code of the semester

        public DateTime StartDate { get; set; } // The official start date of the semester

        public DateTime EndDate { get; set; } // The official end date of the semester

        public bool IsActive { get; set; } // ДОБАВЛЕНО: Indicates if the semester is currently active

        public string? Description { get; set; } // ДОБАВЛЕНО: Optional description of the semester
    }
}
