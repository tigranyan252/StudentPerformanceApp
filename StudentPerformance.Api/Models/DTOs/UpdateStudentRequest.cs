// Path: Models/DTOs/UpdateStudentRequest.cs

using System; // For DateTime type
// using System.ComponentModel.DataAnnotations; // If validation attributes are needed

namespace StudentPerformance.Api.Models.DTOs
{
    // DTO for data sent by the client when requesting to UPDATE an existing student.
    // Includes fields for both the Student entity and UPDATABLE fields of the User entity.
    public class UpdateStudentRequest
    {
        // --- Fields for updating the User entity ---
        // You can update the full name and email of the user associated with the student.
        // [Required(ErrorMessage = "Full name is required for update.")]
        // [StringLength(200)]
        public string FullName { get; set; } = string.Empty; // Updated full name of the student (in the User entity)

        // [EmailAddress] // Example validation
        // [StringLength(150)]
        public string? Email { get; set; } // Updated email of the student (in the User entity, nullable)

        // Login and Password ARE NOT INCLUDED in this DTO for profile updates.


        // --- Fields for updating the Student entity ---
        // For example, date of birth, enrollment date.
        // [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; } // Updated date of birth of the student

        // [DataType(DataType.Date)]
        public DateTime? EnrollmentDate { get; set; } // Updated enrollment date of the student


        // --- Related entities ---
        // Ability to change the group the student belongs to.
        // [Required(ErrorMessage = "Group ID is required for update.")] // Can be required or not, depending on business logic
        public int? GroupId { get; set; } // <<< FIX: Changed to nullable int (int?) - new group of the student


        // !!! Add other fields here if they exist in the Student entity and can be updated !!!

        // !!! DO NOT INCLUDE StudentId or UserId HERE !!! The ID is passed in the URL route (e.g., PUT /api/students/{studentId}).
    }
}