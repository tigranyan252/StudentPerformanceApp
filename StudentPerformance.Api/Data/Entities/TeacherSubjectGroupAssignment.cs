// Path: StudentPerformance.Api/Data/Entities/TeacherSubjectGroupAssignment.cs
using System; // Для DateTime
using System.Collections.Generic; // Для ICollection
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPerformance.Api.Data.Entities
{
    [Table("TeacherSubjectGroupAssignments")]
    public class TeacherSubjectGroupAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeacherSubjectGroupAssignmentId { get; set; }

        [Required]
        public int TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public Teacher Teacher { get; set; } = null!;

        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; } = null!;

        public int? GroupId { get; set; }
        [ForeignKey("GroupId")]
        public Group? Group { get; set; }

        [Required]
        public int SemesterId { get; set; }
        [ForeignKey("SemesterId")]
        public Semester Semester { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

        // ДОБАВЛЕНО: Позволяет получить все оценки, выставленные в рамках этого назначения
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
