// Path: StudentPerformance.Api/Models/DTOs/SubjectDto.cs

namespace StudentPerformance.Api.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for Subject entities.
    /// This DTO represents the basic information of an academic subject.
    /// </summary>
    public class SubjectDto
    {
        public int SubjectId { get; set; } // The unique identifier for the subject

        public string Name { get; set; } = string.Empty; // The full name of the subject (e.g., "Mathematics")

        public string Code { get; set; } = string.Empty; // A short code for the subject (e.g., "MATH101")
    }
}