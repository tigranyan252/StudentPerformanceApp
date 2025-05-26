// Path: StudentPerformance.Api/Utilities/MappingProfile.cs

using AutoMapper;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using System;
using System.Linq; // Add this using directive for .FirstOrDefault() and .Skip()

namespace StudentPerformance.Api.Utilities
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
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name));
            // FIX: Removed .ForMember(dest => dest.FullName, ...)
            // Assuming UserDto does NOT have a FullName property.
            // If you want one, add 'public string? FullName { get; set; }' to UserDto.cs,
            // then you can uncomment this line:
            // .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));

            // Maps a RegisterRequest DTO to a User entity for new user registration.
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Password hashing handled in service!
                .ForMember(dest => dest.RoleId, opt => opt.Ignore());     // RoleId set in service!

            // Maps an AddStudentRequest DTO to a User entity when creating a new student account.
            CreateMap<AddStudentRequest, User>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Login)) // FIX: Map Login (from DTO) to Username (in User entity)
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Password hashing handled in service!
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.FullName.Contains(" ") ? src.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.RoleId, opt => opt.Ignore());     // RoleId for Student set in service!

            // Maps an UpdateStudentRequest DTO to a User entity when updating an existing student account.
            CreateMap<UpdateStudentRequest, User>()
                // FIX: Removed .ForMember(dest => dest.Username, ...) as UpdateStudentRequest does not have a Login/Username property
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Password not updated via this DTO
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.FullName.Contains(" ") ? src.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.RoleId, opt => opt.Ignore()); // RoleId is not updated via this DTO

            // Maps an AddTeacherRequest DTO to a User entity when creating a new teacher account.
            CreateMap<AddTeacherRequest, User>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Login)) // FIX: Map Login (from DTO) to Username (in User entity)
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Password hashing handled in service!
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.FullName.Contains(" ") ? src.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.RoleId, opt => opt.Ignore());     // RoleId for Teacher set in service!

            // Maps an UpdateTeacherRequest DTO to a User entity when updating an existing teacher account.
            CreateMap<UpdateTeacherRequest, User>()
                // FIX: Removed .ForMember(dest => dest.Username, ...) as UpdateTeacherRequest does not have a Login/Username property
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Password not updated via this DTO
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.FullName.Contains(" ") ? src.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.RoleId, opt => opt.Ignore()); // RoleId is not updated via this DTO


            // --- Student Mappings ---
            // Maps a Student entity to a StudentDto, including nested User and Group DTOs.
            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.Group, opt => opt.MapFrom(src => src.Group));

            // Maps an AddStudentRequest DTO to a Student entity for creation or update.
            CreateMap<AddStudentRequest, Student>()
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.EnrollmentDate))
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(dest => dest.UserId, opt => opt.Ignore()); // UserId will be assigned in the service after User creation

            // Maps an UpdateStudentRequest DTO to a Student entity for creation or update.
            CreateMap<UpdateStudentRequest, Student>()
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.EnrollmentDate))
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(dest => dest.UserId, opt => opt.Ignore()); // UserId will be assigned in the service after User creation


            // --- Teacher Mappings ---
            // Maps a Teacher entity to a TeacherDto, including the nested User DTO.
            CreateMap<Teacher, TeacherDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

            // Maps an AddTeacherRequest DTO to a Teacher entity for creation or update.
            CreateMap<AddTeacherRequest, Teacher>()
                .ForMember(dest => dest.TeacherId, opt => opt.Ignore()) // TeacherId is likely auto-generated by DB
                .ForMember(dest => dest.UserId, opt => opt.Ignore()); // UserId set in the service

            // Maps an UpdateTeacherRequest DTO to a Teacher entity for creation or update.
            CreateMap<UpdateTeacherRequest, Teacher>()
                .ForMember(dest => dest.TeacherId, opt => opt.Ignore()) // TeacherId is likely auto-generated by DB
                .ForMember(dest => dest.UserId, opt => opt.Ignore()); // UserId set in the service


            // --- Subject Mappings ---
            // Maps a Subject entity to a SubjectDto.
            CreateMap<Subject, SubjectDto>();
            // Maps an AddSubjectRequest DTO (containing only Name) to a Subject entity.
            CreateMap<AddSubjectRequest, Subject>();
            // If you have an UpdateSubjectRequest, add its mapping
            CreateMap<UpdateSubjectRequest, Subject>();


            // --- Group Mappings ---
            // Maps a Group entity to a GroupDto.
            CreateMap<Group, GroupDto>();
            // Maps an AddGroupRequest DTO to a Group entity.
            CreateMap<AddGroupRequest, Group>();
            // If you have an UpdateGroupRequest, add its mapping
            CreateMap<UpdateGroupRequest, Group>();

            // --- Semester Mappings ---
            // Maps a Semester entity to a SemesterDto.
            CreateMap<Semester, SemesterDto>();
            // Maps an AddSemesterRequest DTO to a Semester entity.
            CreateMap<AddSemesterRequest, Semester>();
            // Maps an UpdateSemesterRequest DTO to a Semester entity.
            CreateMap<UpdateSemesterRequest, Semester>();

            // --- Role Mappings ---
            // Maps a Role entity to a RoleDto.
            CreateMap<Role, RoleDto>();
            // Maps a CreateRoleDto to a Role entity.
            CreateMap<CreateRoleDto, Role>();
            // Maps an UpdateRoleDto to a Role entity.
            CreateMap<UpdateRoleDto, Role>();

            // --- Grade Mappings ---
            // Maps a Grade entity to a GradeDto, including nested DTOs for related entities.
            CreateMap<Grade, GradeDto>()
                .ForMember(dest => dest.Student, opt => opt.MapFrom(src => src.Student))
                .ForMember(dest => dest.Teacher, opt => opt.MapFrom(src => src.Teacher))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.Semester, opt => opt.MapFrom(src => src.Semester));
            // Maps AddGradeRequest and UpdateGradeRequest DTOs to a Grade entity.
            CreateMap<AddGradeRequest, Grade>();
            CreateMap<UpdateGradeRequest, Grade>();

            // --- Assignment Mappings ---
            CreateMap<Assignment, AssignmentDto>()
                // CORRECT: Map Subject and Semester from the nested TeacherSubjectGroupAssignment
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment.Subject))
                .ForMember(dest => dest.Semester, opt => opt.MapFrom(src => src.TeacherSubjectGroupAssignment.Semester));

            // Map AddAssignmentRequest to Assignment
            // TeacherSubjectGroupAssignmentId needs to be resolved and set in the service layer, not by AutoMapper.
            CreateMap<AddAssignmentRequest, Assignment>()
                .ForMember(dest => dest.TeacherSubjectGroupAssignmentId, opt => opt.Ignore());

            // Map UpdateAssignmentRequest to Assignment
            CreateMap<UpdateAssignmentRequest, Assignment>()
                .ForMember(dest => dest.TeacherSubjectGroupAssignmentId, opt => opt.Ignore());

            // --- TeacherSubjectGroupAssignment Mappings ---
            CreateMap<AddTeacherSubjectGroupAssignmentRequest, TeacherSubjectGroupAssignment>();
            CreateMap<UpdateTeacherSubjectGroupAssignmentRequest, TeacherSubjectGroupAssignment>();
            CreateMap<TeacherSubjectGroupAssignment, TeacherSubjectGroupAssignmentDto>()
                .ForMember(dest => dest.Teacher, opt => opt.MapFrom(src => src.Teacher))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.Group, opt => opt.MapFrom(src => src.Group))
                .ForMember(dest => dest.Semester, opt => opt.MapFrom(src => src.Semester));
        }
    }
}