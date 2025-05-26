// Путь: StudentPerformance.Api/Models/DTOs/AddGradeRequest.cs
using System.ComponentModel.DataAnnotations;
using System; // Для DateTime

namespace StudentPerformance.Api.Models.DTOs // Убедитесь, что namespace правильный
{
    public class AddGradeRequest
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int SemesterId { get; set; }

        public int? TeacherId { get; set; }
        public decimal? Value { get; set; }
        public string? ControlType { get; set; }
        public DateTime? DateReceived { get; set; }
        public string? Status { get; set; }
    }
}