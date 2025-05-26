// Path: StudentPerformance.Api/Models/DTOs/AssignmentDto.cs

using System;

namespace StudentPerformance.Api.Models.DTOs
{
    public class AssignmentDto
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public int MaxScore { get; set; }

        // Nested DTOs for navigation properties
        public SubjectDto Subject { get; set; } = null!; // Assuming SubjectDto is already defined
        public SemesterDto Semester { get; set; } = null!; // Assuming SemesterDto is already defined
    }
}