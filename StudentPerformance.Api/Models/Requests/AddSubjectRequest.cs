// Path: Models/DTOs/AddSubjectRequest.cs

using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    // DTO (Data Transfer Object) for data sent by the client
    // when requesting to ADD a new subject.
    public class AddSubjectRequest
    {
        /// <summary>
        /// The name of the subject. This is a required field.
        /// </summary>
        [Required(ErrorMessage = "Subject name is required.")]
        [StringLength(200, ErrorMessage = "Subject name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty; // Имя дисциплины

        /// <summary>
        /// Optional code for the subject.
        /// </summary>
        [StringLength(50, ErrorMessage = "Subject code cannot exceed 50 characters.")]
        public string? Code { get; set; } // Код дисциплины (опционально)

        /// <summary>
        /// Optional description for the subject.
        /// </summary>
        [StringLength(500, ErrorMessage = "Subject description cannot exceed 500 characters.")]
        public string? Description { get; set; } // Описание дисциплины (опционально)
    }
}