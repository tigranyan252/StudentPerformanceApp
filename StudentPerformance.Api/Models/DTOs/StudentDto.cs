// Path: StudentPerformance.Api/Models/DTOs/StudentDto.cs

namespace StudentPerformance.Api.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for Student entities.
    /// This DTO includes basic student details and nested DTOs for their associated
    /// User account and Group.
    /// </summary>
    public class StudentDto
    {
        public int StudentId { get; set; } // The unique identifier for the student profile

        // Foreign Key to the User entity. This links the student profile to their login account.
        public int UserId { get; set; }

        // Foreign Key to the Group entity. This links the student to their academic group.
        // It's nullable in case a student is registered but not yet assigned to a group.
        public int? GroupId { get; set; }

        // --- Nested DTOs for related information ---
        // These properties are populated by AutoMapper, allowing you to include
        // details of the associated User and Group directly within the StudentDto.

        /// <summary>
        /// Information about the associated User account for this student.
        /// </summary>
        public UserDto? User { get; set; }

        /// <summary>
        /// Information about the academic Group this student belongs to.
        /// </summary>
        public GroupDto? Group { get; set; }

        // You might add other properties here that are directly part of the Student entity
        // that you wish to expose, e.g.,
        // public string StudentSpecificProperty { get; set; }
    }
}