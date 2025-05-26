using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs; // Consolidated DTOs to one using statement

namespace StudentPerformance.Api.Services
{
    public interface IUserService
    {
        // --- User Management ---
        Task<AuthenticationResult?> AuthenticateUserAsync(string username, string password);

        Task<User?> RegisterUserAsync(RegisterRequest request);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName);


        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<IEnumerable<UserDto>> GetAllUsersAsync(); // Added for completeness, if used
        Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(int userId);


        // --- Student Management ---
        Task<StudentDto> AddStudentAsync(AddStudentRequest request);
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync(); // Changed to IEnumerable


        Task<StudentDto?> GetStudentDetailsByIdAsync(int studentId);
        Task<bool> UpdateStudentAsync(int studentId, UpdateStudentRequest request);
        Task<bool> DeleteStudentAsync(int studentId);

        // --- Teacher Management ---
        Task<TeacherDto> AddTeacherAsync(AddTeacherRequest request);
        Task<IEnumerable<TeacherDto>> GetAllTeachersAsync(); // Changed to IEnumerable

        Task<TeacherDto?> GetTeacherDetailsByIdAsync(int teacherId);
        Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAssignmentsByTeacherIdAsync(int teacherId);
        Task<bool> UpdateTeacherAsync(int teacherId, UpdateTeacherRequest request);

        Task<bool> DeleteTeacherAsync(int teacherId);

        // --- Subject Management ---
        Task<SubjectDto> AddSubjectAsync(AddSubjectRequest request);

        Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(); // Changed to IEnumerable
        Task<SubjectDto?> GetSubjectByIdAsync(int subjectId);
        Task<bool> UpdateSubjectAsync(int subjectId, UpdateSubjectRequest request);
        Task<bool> DeleteSubjectAsync(int subjectId);

        // --- Assignment Management (TeacherSubjectGroupAssignment) ---
        Task<TeacherSubjectGroupAssignmentDto> AddAssignmentAsync(AddTeacherSubjectGroupAssignmentRequest request);
        Task<TeacherSubjectGroupAssignmentDto?> GetAssignmentByIdAsync(int assignmentId);

        Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAllAssignmentsAsync(); // Changed to IEnumerable
        Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateTeacherSubjectGroupAssignmentRequest request);
        Task<bool> DeleteAssignmentAsync(int assignmentId);

        // --- Authorization Methods ---
        // General User Permissions
        Task<bool> CanUserViewUserDetailsAsync(int currentUserId, int targetUserId);

        Task<bool> CanUserViewUsersByRoleAsync(int currentUserId);
        Task<bool> CanUserUpdateUserAsync(int currentUserId, int targetUserId);
        Task<bool> CanUserDeleteUserAsync(int currentUserId, int targetUserId); // Explicitly added

        Task<bool> CanUserViewAllUsersAsync(int currentUserId); // More general than just by role

        // Assignment Permissions
        Task<bool> CanUserViewAllAssignmentsAsync(int currentUserId);
        Task<bool> CanUserViewAssignmentDetailsAsync(int currentUserId, int assignmentId);
        Task<bool> CanUserAddAssignmentAsync(int currentUserId);
        Task<bool> CanUserUpdateAssignmentAsync(int currentUserId, int assignmentId);
        Task<bool> CanUserDeleteAssignmentAsync(int currentUserId, int assignmentId);

        // Grade Permissions
        Task<bool> CanTeacherAssignGrade(int currentUserId, int studentId, int subjectId, int semesterId);
        Task<bool> CanTeacherUpdateGrade(int currentUserId, int gradeId);
        Task<bool> CanTeacherDeleteGrade(int currentUserId, int gradeId);
        Task<string?> GetUserRoleAsync(int userId);
        Task<TeacherDto?> GetTeacherByIdAsync(int userId); // Assuming TeacherDto exists and maps correctly
        Task<StudentDto?> GetStudentByIdAsync(int userId); // Assuming StudentDto exists and maps correctly
        Task<bool> CanUserViewGrade(int currentUserId, int gradeId);
        Task<bool> CanTeacherViewStudentGrades(int teacherProfileId, int studentId);

        // Group Permissions
        Task<bool> CanUserAddGroupAsync(int currentUserId);
        Task<bool> CanUserUpdateGroupAsync(int currentUserId, int groupId);
        Task<bool> CanUserDeleteGroupAsync(int currentUserId, int groupId);
        Task<bool> CanUserViewGroupAsync(int currentUserId, int groupId);
        Task<bool> CanUserViewAllGroupsAsync(int currentUserId);

        // Semester Permissions

        Task<IEnumerable<SemesterDto>> GetAllSemestersAsync();
        Task<SemesterDto?> GetSemesterByIdAsync(int semesterId);
        Task<SemesterDto?> AddSemesterAsync(AddSemesterRequest request);
        Task<bool> UpdateSemesterAsync(int semesterId, UpdateSemesterRequest request);
        Task<bool> DeleteSemesterAsync(int semesterId);

        // Semester-related authorization methods (ensure these are added)
        Task<bool> CanUserViewAllSemestersAsync(int currentUserId);
        Task<bool> CanUserViewSemesterDetailsAsync(int currentUserId, int semesterId);
        Task<bool> CanUserAddSemesterAsync(int currentUserId);
        Task<bool> CanUserUpdateSemesterAsync(int currentUserId, int semesterId);
        Task<bool> CanUserDeleteSemesterAsync(int currentUserId, int semesterId);

        // Role-related authorization methods (ADD THESE if they are not already there!)
        Task<bool> CanUserViewAllRolesAsync(int currentUserId);
        Task<bool> CanUserViewRoleDetailsAsync(int currentUserId, int roleId);
        Task<bool> CanUserCreateRoleAsync(int currentUserId);
        Task<bool> CanUserUpdateRoleAsync(int currentUserId, int roleId); // <<< THIS LINE IS THE KEY CHANGE
        Task<bool> CanUserDeleteRoleAsync(int currentUserId, int roleId);


        // Student Permissions
        Task<bool> CanUserAddStudentAsync(int currentUserId);
        Task<bool> CanUserViewAllStudentsAsync(int currentUserId);
        Task<bool> CanUserViewStudentDetailsAsync(int currentUserId, int studentId);
        Task<bool> CanUserUpdateStudentAsync(int currentUserId, int studentId);
        Task<bool> CanUserDeleteStudentAsync(int currentUserId, int studentId);

        // Teacher Permissions
        Task<bool> CanUserAddTeacherAsync(int currentUserId);
        Task<bool> CanUserViewAllTeachersAsync(int currentUserId);
        Task<bool> CanUserViewTeacherDetailsAsync(int currentUserId, int teacherId);
        Task<bool> CanUserUpdateTeacherAsync(int currentUserId, int teacherId);
        Task<bool> CanUserDeleteTeacherAsync(int currentUserId, int teacherId);

        // Subject Permissions
        Task<bool> CanUserAddSubjectAsync(int currentUserId);
        Task<bool> CanUserUpdateSubjectAsync(int currentUserId, int subjectId);
        Task<bool> CanUserDeleteSubjectAsync(int currentUserId, int subjectId);
        Task<bool> CanUserViewSubjectAsync(int currentUserId, int subjectId);
        Task<bool> CanUserViewSubjectDetailsAsync(int currentUserId, int subjectId);
        Task<bool> CanUserViewAllSubjectsAsync(int currentUserId);
    }
}