// Path: StudentPerformance.Api/Models/DTOs/UpdateSubjectRequest.cs

using System;
// using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    public class UpdateSubjectRequest
    {
        // FIX: Ensure this property is named 'Name'
        // [Required(ErrorMessage = "Subject name is required for update.")]
        // [StringLength(200)]
        public string Name { get; set; } = string.Empty; // Updated Subject Name

        // [StringLength(50)]
        public string? Code { get; set; }

        // [StringLength(500)]
        public string? Description { get; set; }

        // Other properties if any
    }
}