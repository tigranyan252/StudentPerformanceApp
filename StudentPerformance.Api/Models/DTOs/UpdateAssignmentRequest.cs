// Path: StudentPerformance.Api/Models/DTOs/UpdateAssignmentRequest.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.DTOs
{
    public class UpdateAssignmentRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Maximum score is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Max score must be a positive number.")]
        public int MaxScore { get; set; }

        [Required(ErrorMessage = "Subject ID is required.")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Semester ID is required.")]
        public int SemesterId { get; set; }
    }
}