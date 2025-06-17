// Path: StudentPerformance.Api/Utilities/MappingProfile.cs

using AutoMapper;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using System;
using System.Linq;

namespace StudentPerformance.Api
{
    /// <summary>
    /// Configures AutoMapper to define how data is transferred between
    /// database entities and Data Transfer Objects (DTOs).
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- User Mappings ---
            // Maps a User entity to a UserDto for read operations.
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : null))
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : null))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()))
                // Mapping fields specific to Student, if User is a student
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.Student != null ? src.Student.GroupId : (int?)null))
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Student != null && src.Student.Group != null ? src.Student.Group.Name : null))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.Student != null ? src.Student.DateOfBirth : (DateTime?)null))
                .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.Student != null ? src.Student.EnrollmentDate : (DateTime?)null))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentId : (int?)null))
                // Mapping fields specific to Teacher, if User is a teacher
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.Department : null))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.Position : null))
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.TeacherId : (int?)null));

            // Maps a RegisterRequest DTO to a User entity for new user registration.
            // PasswordHash and RoleId will be handled manually in UserService.
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore()) // Role set in service
                .ForMember(dest => dest.Student, opt => opt.Ignore()) // Student created/assigned in service
                .ForMember(dest => dest.Teacher, opt => opt.Ignore()); // Teacher created/assigned in service


            // Maps an UpdateUserRequest DTO to a User entity for updating an existing user.
            // PasswordHash and RoleId/Role will be handled manually in UserService.
            CreateMap<UpdateUserRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.Teacher, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null)); // Update only non-null fields

            // --- ДОБАВЛЕНО: Маппинг AddTeacherRequest к сущности User ---
            // Этот маппинг необходим, потому что AddTeacherRequest содержит поля для создания User (пользователя)
            // PasswordHash, RoleId/Role и навигационные свойства будут обрабатываться вручную в UserService или TeacherService.
            CreateMap<AddTeacherRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Пароль будет хеширован сервисом
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())       // Роль будет установлена сервисом (например, через RoleId по имени "Teacher")
                .ForMember(dest => dest.Role, opt => opt.Ignore())         // Игнорируем навигационное свойство Role
                .ForMember(dest => dest.Student, opt => opt.Ignore())      // Игнорируем, так как это Teacher, а не Student
                .ForMember(dest => dest.Teacher, opt => opt.Ignore());     // Игнорируем, так как сущность Teacher будет создаваться отдельно и связываться с этим User


            // --- Student Mappings ---
            // Maps a Student entity to a StudentDto.
            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Group != null ? src.Group.Name : null))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.EnrollmentDate))
                // Mapping fields from the associated User entity into a flat StudentDto
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User != null ? src.User.FirstName : null))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User != null ? src.User.LastName : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : null))
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.User != null && src.User.Role != null ? src.User.Role.Name : null));


            // Maps AddStudentRequest DTO to a Student entity.
            CreateMap<AddStudentRequest, Student>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore()); // UserId set in UserService


            // Maps UpdateStudentRequest DTO to a Student entity.
            CreateMap<UpdateStudentRequest, Student>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // UserId ignored on update
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null)); // Update only non-null fields

            // --- Teacher Mappings ---
            // Maps a Teacher entity to a TeacherDto.
            CreateMap<Teacher, TeacherDto>()
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.TeacherId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                // Mapping fields from the associated User entity into a flat TeacherDto
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User != null ? src.User.FirstName : null))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User != null ? src.User.LastName : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : null))
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.User != null && src.User.Role != null ? src.User.Role.Name : null))
                .ForMember(dest => dest.HireDate, opt => opt.Ignore());


            // Maps AddTeacherRequest DTO to a Teacher entity.
            CreateMap<AddTeacherRequest, Teacher>()
                .ForMember(dest => dest.TeacherId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore()); // UserId set in UserService

            // Maps UpdateTeacherRequest DTO to a Teacher entity.
            CreateMap<UpdateTeacherRequest, Teacher>()
                .ForMember(dest => dest.TeacherId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));


            // --- Subject Mappings ---
            CreateMap<Subject, SubjectDto>();
            CreateMap<AddSubjectRequest, Subject>();
            CreateMap<UpdateSubjectRequest, Subject>();


            // --- Group Mappings ---
            CreateMap<Group, GroupDto>()
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Name)) // Маппим из Group.Name
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));

            CreateMap<AddGroupRequest, Group>();
            CreateMap<UpdateGroupRequest, Group>();

            // --- Semester Mappings ---
            CreateMap<Semester, SemesterDto>();
            CreateMap<AddSemesterRequest, Semester>();
            CreateMap<UpdateSemesterRequest, Semester>();

            // --- Role Mappings ---
            CreateMap<Role, RoleDto>();
            CreateMap<CreateRoleDto, Role>();
            CreateMap<UpdateRoleDto, Role>();


            // --- Grade Mappings ---
            CreateMap<Grade, GradeDto>()
                .ForMember(dest => dest.GradeId, opt => opt.MapFrom(src => src.GradeId))
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
                .ForMember(dest => dest.DateReceived, opt => opt.MapFrom(src => src.DateReceived))
                .ForMember(dest => dest.ControlType, opt => opt.MapFrom(src => src.ControlType))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                // Mapping TeacherId, SubjectId, SemesterId via TeacherSubjectGroupAssignment
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment != null ? src.TeacherSubjectGroupAssignment.TeacherId : (int?)null))
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment != null ? src.TeacherSubjectGroupAssignment.SubjectId : (int?)null))
                .ForMember(dest => dest.SemesterId, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment != null ? src.TeacherSubjectGroupAssignment.SemesterId : (int?)null))
                .ForMember(dest => dest.AssignmentId, opt => opt.MapFrom(src => src.AssignmentId))
                .ForMember(dest => dest.AssignmentTitle, opt => opt.MapFrom(src => src.Assignment != null ? src.Assignment.Title : null))

                // Mapping flat names for table display (VERY IMPORTANT)
                // These mappings require corresponding Includes to be performed in GradeService.
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? $"{src.Student.User.FirstName} {src.Student.User.LastName}".Trim() : null))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment != null && src.TeacherSubjectGroupAssignment.Teacher != null && src.TeacherSubjectGroupAssignment.Teacher.User != null ? $"{src.TeacherSubjectGroupAssignment.Teacher.User.FirstName} {src.TeacherSubjectGroupAssignment.Teacher.User.LastName}".Trim() : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment != null && src.TeacherSubjectGroupAssignment.Subject != null ? src.TeacherSubjectGroupAssignment.Subject.Name : null))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment != null && src.TeacherSubjectGroupAssignment.Semester != null ? src.TeacherSubjectGroupAssignment.Semester.Name : null));

            CreateMap<AddGradeRequest, Grade>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.GradeId, opt => opt.Ignore());

            CreateMap<UpdateGradeRequest, Grade>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));


            // --- Assignment Mappings (for GENERAL assignments: Homework, Project) ---
            CreateMap<Assignment, AssignmentDto>()
                // Explicitly map Subject and Semester from TeacherSubjectGroupAssignment
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment != null ? src.TeacherSubjectGroupAssignment.Subject : null))
                .ForMember(dest => dest.Semester, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment != null ? src.TeacherSubjectGroupAssignment.Semester : null));

            CreateMap<AddAssignmentRequest, Assignment>()
                .ForMember(dest => dest.TeacherSubjectGroupAssignmentId, opt => opt.Ignore());

            CreateMap<UpdateAssignmentRequest, Assignment>()
                .ForMember(dest => dest.TeacherSubjectGroupAssignmentId, opt => opt.Ignore());

            // --- TeacherSubjectGroupAssignment Mappings (for ASSIGNMENTS) ---
            CreateMap<AddTeacherSubjectGroupAssignmentRequest, TeacherSubjectGroupAssignment>();
            CreateMap<UpdateTeacherSubjectGroupAssignmentRequest, TeacherSubjectGroupAssignment>();
            CreateMap<TeacherSubjectGroupAssignment, TeacherSubjectGroupAssignmentDto>()
                // Ensure Teacher, Subject, Group, Semester are loaded in the entity
                .ForMember(dest => dest.Teacher, opt => opt.MapFrom(src => src.Teacher))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.Group, opt => opt.MapFrom(src => src.Group))
                .ForMember(dest => dest.Semester, opt => opt.MapFrom(src => src.Semester));
        }
    }
}
