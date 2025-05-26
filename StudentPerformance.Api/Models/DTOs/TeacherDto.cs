// Path: StudentPerformance.Api/Models/DTOs/TeacherDto.cs

namespace StudentPerformance.Api.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for returning Teacher information.
    /// </summary>
    public class TeacherDto
    {
        public int TeacherId { get; set; }
        public int UserId { get; set; }
        public UserDto User { get; set; } = null!; // Include user details of the teacher
        // Add other teacher-specific properties here if your Teacher entity has them
        // public DateTime? HireDate { get; set; }
        // public string? Specialization { get; set; }
    }
}