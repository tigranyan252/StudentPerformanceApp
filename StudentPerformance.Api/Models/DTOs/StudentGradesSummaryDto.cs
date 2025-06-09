// StudentPerformance.Api/Models/DTOs/StudentGradesSummaryDto.cs

namespace StudentPerformance.Api.Models.DTOs
{
    public class StudentGradesSummaryDto
    {
        public int StudentId { get; set; }
        // НОВЫЕ ПОЛЯ
        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
        // Конец новых полей

        public int SubjectId { get; set; }
        // Возможно, также захотим добавить SubjectName сюда, чтобы не искать на фронтенде
        // public string SubjectName { get; set; } 

        public double AverageGrade { get; set; }
        public int GradeCount { get; set; }
    }
}
