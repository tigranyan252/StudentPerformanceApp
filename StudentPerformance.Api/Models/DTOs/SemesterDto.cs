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
        public int SemesterId { get; set; } // The unique identifier for the semester

        public string Name { get; set; } = string.Empty; // The name of the semester (e.g., "Fall 2024", "Spring 2025")

        public DateTime StartDate { get; set; } // The official start date of the semester

        public DateTime EndDate { get; set; } // The official end date of the semester
    }
}