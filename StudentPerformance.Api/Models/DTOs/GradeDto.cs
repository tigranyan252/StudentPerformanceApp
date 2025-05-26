// Path: StudentPerformance.Api/Models/DTOs/GradeDto.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for Grade entities, including related entity details.
    /// </summary>
    public class GradeDto
    {
        public int GradeId { get; set; }

        public int Value { get; set; } // The actual grade value

        public DateTime DateAssigned { get; set; } // When the grade was assigned

        // Foreign Key IDs (optional, but often useful for simpler DTOs or debugging)
        public int StudentId { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public int SemesterId { get; set; }

        // Navigation Properties as DTOs for displaying related information
        // These are the properties that the AutoMapper.ForMember calls are trying to map to.
        public StudentDto? Student { get; set; }
        public TeacherDto? Teacher { get; set; }
        public SubjectDto? Subject { get; set; }
        public SemesterDto? Semester { get; set; }
    }
}