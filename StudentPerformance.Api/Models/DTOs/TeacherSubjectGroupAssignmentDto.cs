// Path: StudentPerformance.Api/Models/DTOs/TeacherSubjectGroupAssignmentDto.cs





namespace StudentPerformance.Api.Models.DTOs

{

    /// <summary>

    /// DTO for returning details of a Teacher-Subject-Group-Semester Assignment.

    /// </summary>

    public class TeacherSubjectGroupAssignmentDto

    {

        public int TeacherSubjectGroupAssignmentId { get; set; } // The unique ID of the assignment



        // Include DTOs for related entities to provide full details

        public int TeacherId { get; set; }

        public TeacherDto? Teacher { get; set; } // Details of the assigned teacher



        public int SubjectId { get; set; }

        public SubjectDto? Subject { get; set; } // Details of the assigned subject



        public int GroupId { get; set; }

        public GroupDto? Group { get; set; } // Details of the assigned group



        public int SemesterId { get; set; }

        public SemesterDto? Semester { get; set; } // Details of the assigned semester



        // Add any other properties from your TeacherSubjectGroupAssignment entity

        // that you want to expose in the DTO, e.g.,

        // public DateTime? StartDate { get; set; }

        // public DateTime? EndDate { get; set; }

    }

}