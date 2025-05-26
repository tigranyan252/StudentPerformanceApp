// Path: Models/DTOs/AddStudentRequest.cs

using System; // For DateTime types
// using System.ComponentModel.DataAnnotations; // If validation attributes are needed

namespace StudentPerformance.Api.Models.DTOs
{
    // DTO for data sent by the client when requesting to ADD a new student.
    // Includes fields for both the User and Student entities.
    public class AddStudentRequest
    {
        // --- Fields for the User entity ---
        // [Required(ErrorMessage = "Login is required for a new student's user account.")]
        // [StringLength(50)]
        public string Login { get; set; } = string.Empty; // Login for the new user

        // [Required(ErrorMessage = "Password is required for a new student's user account.")]
        // [StringLength(100)]
        public string Password { get; set; } = string.Empty; // Password for the new user (will be hashed in the service!)

        // [Required(ErrorMessage = "Full name is required for a new student.")]
        // [StringLength(200)]
        public string FullName { get; set; } = string.Empty; // Full name of the student (will be in the User entity)

        // [EmailAddress] // Example of email format validation
        // [StringLength(150)]
        public string? Email { get; set; } // Student's email (in User entity, nullable)

        // RoleId or RoleName are typically not specified here; the "Student" role is automatically set by the service during creation.


        // --- Fields for the Student entity ---
        // For example, date of birth, enrollment date.
        // [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; } // Student's date of birth

        // [DataType(DataType.Date)]
        public DateTime? EnrollmentDate { get; set; } // Student's enrollment date


        // --- Related entities ---
        // ID of the group the student belongs to.
        // [Required(ErrorMessage = "Group ID is required for a new student.")] // <<< This validation might need adjustment if GroupId is truly optional.
        public int? GroupId { get; set; } // <<< FIX: Changed to nullable int (int?)

        // !!! Add other fields here if they exist in the Student entity and are needed for adding !!!
    }
}