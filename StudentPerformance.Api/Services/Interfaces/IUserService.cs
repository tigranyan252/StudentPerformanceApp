// Path: StudentPerformance.Api/Services/Interfaces/IUserService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;

namespace StudentPerformance.Api.Services.Interfaces
{
    public interface IUserService
    {
        // --- Core User Authentication & Management ---
        Task<AuthenticationResult?> AuthenticateUserAsync(string username, string password);
        Task<UserDto?> RegisterUserAsync(RegisterRequest request);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);

        // General User Read Operations
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<IEnumerable<UserDto>> GetAllUsersAsync(string? username, string? userType);
        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName);
        Task<string?> GetUserRoleAsync(int userId); // Получение роли пользователя по ID

        // General User Write Operations
        Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(int userId);

        // --- Profile Retrieval (for specific roles) ---
        Task<StudentDto?> GetStudentByIdAsync(int userId); // Получает профиль студента по User ID
        Task<TeacherDto?> GetTeacherByIdAsync(int userId); // Получает профиль преподавателя по User ID

        // #######################################################################
        // --- Centralized Authorization / Permission Checks ---
        // Эти методы проверяют, имеет ли текущий пользователь (по userId) право
        // выполнять определенное действие.
        // Используют GetUserRoleAsync и, при необходимости, контекст конкретной сущности.
        // #######################################################################

        // Permissions for User entities themselves
        Task<bool> CanUserViewAllUsersAsync(int currentUserId);
        Task<bool> CanUserViewUserDetailsAsync(int currentUserId, int targetUserId);
        Task<bool> CanUserUpdateUserAsync(int currentUserId, int targetUserId);
        Task<bool> CanUserDeleteUserAsync(int currentUserId, int targetUserId);
        // ДОБАВЛЕНО: Объявление метода CanUserManageUsersAsync в интерфейсе
        Task<bool> CanUserManageUsersAsync(int currentUserId);

        // Permissions for Student entities
        Task<bool> CanUserAddStudentAsync(int currentUserId);
        Task<bool> CanUserViewAllStudentsAsync(int currentUserId);
        Task<bool> CanUserViewStudentDetailsAsync(int currentUserId, int studentId);
        Task<bool> CanUserUpdateStudentAsync(int currentUserId, int studentId);
        Task<bool> CanUserDeleteStudentAsync(int currentUserId, int studentId);

        // Permissions for Teacher entities
        Task<bool> CanUserAddTeacherAsync(int currentUserId);
        Task<bool> CanUserViewAllTeachersAsync(int currentUserId);
        Task<bool> CanUserViewTeacherDetailsAsync(int currentUserId, int teacherId);
        Task<bool> CanUserUpdateTeacherAsync(int currentUserId, int teacherId);
        Task<bool> CanUserDeleteTeacherAsync(int currentUserId, int teacherId);

        // Permissions for Subject entities
        Task<bool> CanUserAddSubjectAsync(int currentUserId);
        Task<bool> CanUserViewAllSubjectsAsync(int currentUserId);
        Task<bool> CanUserViewSubjectDetailsAsync(int currentUserId, int subjectId);
        Task<bool> CanUserUpdateSubjectAsync(int currentUserId, int subjectId);
        Task<bool> CanUserDeleteSubjectAsync(int currentUserId, int subjectId);

        // Permissions for Group entities
        Task<bool> CanUserAddGroupAsync(int currentUserId);
        Task<bool> CanUserViewAllGroupsAsync(int currentUserId);
        Task<bool> CanUserViewGroupDetailsAsync(int currentUserId, int groupId);
        Task<bool> CanUserUpdateGroupAsync(int currentUserId, int groupId);
        Task<bool> CanUserDeleteGroupAsync(int currentUserId, int groupId);

        // Permissions for Semester entities
        Task<bool> CanUserAddSemesterAsync(int currentUserId);
        Task<bool> CanUserViewAllSemestersAsync(int currentUserId);
        Task<bool> CanUserViewSemesterDetailsAsync(int currentUserId, int semesterId);
        Task<bool> CanUserUpdateSemesterAsync(int currentUserId, int semesterId);
        Task<bool> CanUserDeleteSemesterAsync(int currentUserId, int semesterId);

        // Permissions for TeacherSubjectGroupAssignment (Assignments) entities
        Task<bool> CanUserAddAssignmentAsync(int currentUserId);
        Task<bool> CanUserViewAllAssignmentsAsync(int currentUserId);
        Task<bool> CanUserViewAssignmentDetailsAsync(int currentUserId, int assignmentId);
        Task<bool> CanUserUpdateAssignmentAsync(int currentUserId, int assignmentId);
        Task<bool> CanUserDeleteAssignmentAsync(int currentUserId, int assignmentId);
        Task<bool> IsUserAssignedToTeacherSubjectGroupAssignment(int userId, int teacherSubjectGroupAssignmentId);
        Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAssignmentsByTeacherIdAsync(int teacherId);

        // Permissions for Grade entities
        Task<bool> CanUserAddGradeAsync(int currentUserId);
        Task<bool> CanUserViewAllGradesAsync(int currentUserId);
        Task<bool> CanUserViewGradeDetailsAsync(int currentUserId, int gradeId);
        Task<bool> CanUserUpdateGradeAsync(int currentUserId, int gradeId);
        Task<bool> CanUserDeleteGradeAsync(int currentUserId, int gradeId);
        Task<bool> CanTeacherAssignGrade(int currentUserId, int studentId, int subjectId, int semesterId);
        Task<bool> CanTeacherViewStudentGrades(int teacherProfileId, int studentId);

        // Permissions for Role entities
        Task<bool> CanUserViewAllRolesAsync(int currentUserId);
        Task<bool> CanUserViewRoleDetailsAsync(int currentUserId, int roleId);
        Task<bool> CanUserCreateRoleAsync(int currentUserId);
        Task<bool> CanUserUpdateRoleAsync(int currentUserId, int roleId);
        Task<bool> CanUserDeleteRoleAsync(int currentUserId, int roleId);
    }
}
