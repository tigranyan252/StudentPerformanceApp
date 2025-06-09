// Path: Models/DTOs/AddStudentRequest.cs

using System;
using System.ComponentModel.DataAnnotations; // Раскомментируйте эту строку

namespace StudentPerformance.Api.Models.Requests
{
    // DTO for data sent by the client when requesting to ADD a new student.
    // Includes fields for both the User and Student entities.
    public class AddStudentRequest
    {
        // --- Fields for the User entity ---
        [Required(ErrorMessage = "Username is required for a new student's user account.")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
        public string Username { get; set; } = string.Empty; // Изменено с Login на Username

        [Required(ErrorMessage = "Password is required for a new student's user account.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required for a new student.")]
        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty; // ДОБАВЛЕНО

        [Required(ErrorMessage = "Last Name is required for a new student.")]
        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty; // ДОБАВЛЕНО

        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; }

        // RoleId or RoleName are typically not specified here; the "Student" role is automatically set by the service during creation.


        // --- Fields for the Student entity ---
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EnrollmentDate { get; set; }


        // --- Related entities ---
        // ID of the group the student belongs to.
        [Required(ErrorMessage = "Group is required for a new student.")]
        public int? GroupId { get; set; } // Оставлено как nullable int, если группа может быть необязательной
    }
}