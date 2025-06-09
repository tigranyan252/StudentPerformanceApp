// Path: StudentPerformance.Api/Models/DTOs/RegisterRequest.cs (Corrected Path)

using System; // Добавлено для DateTime
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    /// <summary>
    /// Data Transfer Object for user registration requests.
    /// This DTO carries the necessary information from the client to register a new user.
    /// </summary>
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        // In a real application, consider using a more robust password policy (e.g., regex for complexity)
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "User type is required.")]
        // Example: "Student", "Teacher", "Admin"
        public string UserType { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; } // Email is optional in the database, but required for format if provided

        // Properties for Student profile (nullable)
        // Эти поля будут заполняться только при регистрации пользователя с UserType="Student"
        public int? GroupId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? EnrollmentDate { get; set; }

        // Properties for Teacher profile (nullable)
        // Эти поля будут заполняться только при регистрации пользователя с UserType="Teacher"
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
        public string? Department { get; set; }

        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters.")] // ИСПРАВЛЕНО: ErrorError -> ErrorMessage
        public string? Position { get; set; }
    }
}
