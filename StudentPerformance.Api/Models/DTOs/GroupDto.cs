// Path: StudentPerformance.Api/Models/DTOs/GroupDto.cs

namespace StudentPerformance.Api.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for Group entities.
    /// This DTO represents the basic information of an academic group or class.
    /// </summary>
    public class GroupDto
    {
        public int GroupId { get; set; } // The unique identifier for the group

        public string Name { get; set; } = string.Empty; // The full name of the group (e.g., "Software Engineering 2024")

        public string Code { get; set; } = string.Empty; // A short code for the group (e.g., "SE2024A")
    }
}