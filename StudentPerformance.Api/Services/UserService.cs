// Path: StudentPerformance.Api/Services/UserService.cs

using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using StudentPerformance.Api.Models.QueryParameters;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StudentPerformance.Api.Models.Requests;
using System.Reflection;
using Microsoft.Extensions.Logging;
using StudentPerformance.Api.Exceptions;
using StudentPerformance.Api.Utilities;
using System.Collections.Generic;
using System.Linq;
using System;
using BCrypt.Net;
using StudentPerformance.Api.Services.Interfaces;

namespace StudentPerformance.Api.Services
{
    /// <summary>
    /// Provides services for user authentication, role management, and granular authorization checks.
    /// In a larger application, consider breaking this into smaller, more focused services
    /// (e.g., AuthService, GroupService, AssignmentService, GradeService, SemesterService, StudentService, SubjectService, TeacherService).
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context,
                           IPasswordHasher<User> passwordHasher,
                           IMapper mapper,
                           IConfiguration configuration,
                           ILogger<UserService> logger)
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _logger = logger;
        }

        // --- Authentication & Registration Methods ---
        public async Task<AuthenticationResult?> AuthenticateUserAsync(string username, string password)
        {
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .Include(u => u.Student) // Включаем профиль студента для доступа к GroupId
                                     .Include(u => u.Teacher) // Включаем профиль преподавателя для доступа к Department/Position
                                     .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Success)
            {
                _logger.LogWarning($"Authentication failed for user {username}: Invalid credentials.");
                return new AuthenticationResult { Success = false, Errors = new[] { "Invalid credentials" } };
            }

            var token = GenerateJwtToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            _logger.LogInformation($"User {username} authenticated successfully.");

            return new AuthenticationResult
            {
                User = userDto,
                Token = token,
                Success = true
            };
        }

        public async Task<UserDto?> RegisterUserAsync(RegisterRequest request)
        {
            _logger.LogInformation($"Attempting to register new user with username: {request.Username}, UserType: {request.UserType}");

            try
            {
                // Ensure username is unique
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    _logger.LogWarning($"Registration failed: Username '{request.Username}' is already taken.");
                    throw new ConflictException($"Username '{request.Username}' is already taken.");
                }
                // Ensure email is unique if provided
                if (!string.IsNullOrWhiteSpace(request.Email) && await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    _logger.LogWarning($"Registration failed: Email '{request.Email}' already taken.");
                    throw new ConflictException($"Email '{request.Email}' is already taken.");
                }

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.UserType);
                if (role == null)
                {
                    _logger.LogWarning($"Registration failed: UserType '{request.UserType}' is not a valid role.");
                    throw new BadRequestException($"UserType '{request.UserType}' is not a valid role.");
                }

                var newUser = new User
                {
                    Username = request.Username,
                    RoleId = role.RoleId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Assume _passwordHasher is correctly configured and working
                newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);

                _context.Users.Add(newUser);
                _logger.LogInformation($"User entity created for {request.Username}. Saving to DB (first save for User Id)...");
                await _context.SaveChangesAsync(); // First save to get the new User.Id
                _logger.LogInformation($"User {newUser.Id} saved successfully. Processing profile creation...");


                if (role.Name == UserRoles.Student)
                {
                    // GroupId is required for Student in RegisterRequest (int?) and Student entity (int)
                    if (!request.GroupId.HasValue)
                    {
                        _logger.LogWarning($"Registration failed for student '{request.Username}': GroupId is required but was not provided.");
                        throw new BadRequestException("GroupId is required for student registration.");
                    }

                    var group = await _context.Groups.FindAsync(request.GroupId.Value);
                    if (group == null)
                    {
                        _logger.LogWarning($"Registration failed for student '{request.Username}': Group with ID '{request.GroupId.Value}' not found.");
                        throw new NotFoundException($"Group with ID '{request.GroupId.Value}' not found.");
                    }

                    // DateOfBirth and EnrollmentDate are required for Student in RegisterRequest (DateTime?) and Student entity (DateTime?)
                    if (!request.DateOfBirth.HasValue)
                    {
                        _logger.LogWarning($"Registration failed for student '{request.Username}': DateOfBirth is required.");
                        throw new BadRequestException("DateOfBirth is required for student registration.");
                    }
                    if (!request.EnrollmentDate.HasValue)
                    {
                        _logger.LogWarning($"Registration failed for student '{request.Username}': EnrollmentDate is required.");
                        throw new BadRequestException("EnrollmentDate is required for student registration.");
                    }

                    var studentProfile = new Student
                    {
                        UserId = newUser.Id,
                        GroupId = request.GroupId.Value, // Use .Value here for non-nullable int entity property
                        DateOfBirth = request.DateOfBirth.Value, // Use .Value here for non-nullable DateTime entity property (if entity is DateTime)
                        EnrollmentDate = request.EnrollmentDate.Value, // Use .Value here for non-nullable DateTime entity property (if entity is DateTime)
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true // Default value, assuming it's in your Student entity
                    };
                    _context.Students.Add(studentProfile);
                    _logger.LogInformation($"Student profile created for user {newUser.Id}. Saving profile to DB (second save)...");
                }
                else if (role.Name == UserRoles.Teacher)
                {
                    var teacherProfile = new Teacher
                    {
                        UserId = newUser.Id,
                        Department = request.Department,
                        Position = request.Position,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Teachers.Add(teacherProfile);
                    _logger.LogInformation($"Teacher profile created for user {newUser.Id}. Saving profile to DB (second save)...");
                }
                // Add other role profiles here if applicable

                await _context.SaveChangesAsync(); // Save the student/teacher profile
                _logger.LogInformation($"User profile (if any) saved successfully for user {newUser.Id}.");


                // CRITICAL FIX: Re-fetch the user with all necessary includes for DTO mapping
                _logger.LogInformation($"Re-fetching user {newUser.Id} with all related data for DTO mapping...");
                var registeredUserWithProfiles = await _context.Users
                                                             .Include(u => u.Role)
                                                             .Include(u => u.Student)
                                                                 .ThenInclude(s => s.Group) // REQUIRED for GroupName in UserDto
                                                             .Include(u => u.Teacher)
                                                             .FirstOrDefaultAsync(u => u.Id == newUser.Id);

                if (registeredUserWithProfiles == null)
                {
                    _logger.LogError($"Re-fetch of user {newUser.Id} failed after successful save. This is an unexpected state.");
                    throw new InvalidOperationException($"Failed to retrieve user {newUser.Id} details after registration. Data might be saved, but response could not be generated.");
                }

                _logger.LogInformation($"User {newUser.Id} successfully re-fetched. Attempting to map to UserDto...");
                var userDto = _mapper.Map<UserDto>(registeredUserWithProfiles);
                _logger.LogInformation($"Mapping to UserDto successful for user {newUser.Id}.");

                _logger.LogInformation($"User '{request.Username}' registered successfully with role '{role.Name}'. Returning DTO.");
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred during user registration for username: {request.Username}. Details: {ex.Message}");
                // Re-throw the exception to ensure the API returns a 500 with details
                // This is where your global exception handler (if configured) would catch it.
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            _logger.LogInformation($"Attempting to change password for user ID: {userId}");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning($"Password change failed: User with ID {userId} not found.");
                throw new NotFoundException($"User with ID {userId} not found.");
            }

            if (!string.IsNullOrEmpty(request.OldPassword))
            {
                if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword) != PasswordVerificationResult.Success)
                {
                    _logger.LogWarning($"Password change failed for user {userId}: Invalid old password.");
                    throw new ArgumentException("Invalid old password.");
                }
            }
            else
            {
                _logger.LogWarning($"Password change for user {userId}: Old password was not provided. Proceeding without old password verification. Ensure this is an authorized operation.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Password for user {userId} changed successfully.");
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency conflict occurred while changing password for user ID {userId}. Checking if user still exists...");
                if (!_context.Users.Any(e => e.Id == userId))
                {
                    _logger.LogWarning($"User {userId} was deleted by another user during a concurrency conflict while changing password.");
                    throw new NotFoundException($"User with ID {userId} was deleted by another user.");
                }
                _logger.LogError(ex, $"Concurrency conflict occurred while changing password for user ID {userId}, but user still exists.");
                throw new ConflictException($"Concurrency conflict when changing password for user with ID {userId}. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during password change for user ID {userId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while changing password for user with ID {userId}.", ex);
            }
        }

        // --- General User Read Operations ---
        public async Task<bool> IsUserAdmin(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            return user?.Role?.Name == UserRoles.Administrator;
        }
        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .Include(u => u.Student) // For GroupId in UserDto
                                         .ThenInclude(s => s.Group) // If group name/details are needed
                                     .Include(u => u.Teacher) // For Department/Position in UserDto
                                     .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("User profile not found for user ID {UserId}.", (object)userId);
            }
            else
            {
                _logger.LogInformation("User profile found for user ID {UserId}.", (object)userId);
            }
            return _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(string? username, string? userType)
        {
            _logger.LogInformation("Attempting to retrieve all users with filters: Username={Username}, UserType={UserType}", (object)username, (object)userType);

            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Student)
                    .ThenInclude(s => s.Group)
                .Include(u => u.Teacher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(u => u.Username.Contains(username));
            }

            if (!string.IsNullOrWhiteSpace(userType))
            {
                query = query.Where(u => u.Role != null && u.Role.Name == userType);
            }

            var users = await query.ToListAsync();
            _logger.LogInformation("Fetched {Count} users with filters username='{Username}', userType='{UserType}'.", (object)users.Count, (object)username, (object)userType);

            // Важно: Map на List, но возвращаем IEnumerable
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName)
        {
            var users = await _context.Users
                                      .Include(u => u.Role)
                                      .Include(u => u.Student) // To include GroupId in UserDto mapping
                                      .Where(u => u.Role != null && u.Role.Name == roleName)
                                      .ToListAsync();
            _logger.LogInformation($"{users.Count} users retrieved for role {roleName}.");
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<string?> GetUserRoleAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogInformation($"User role not found for user ID {userId} (user not found).");
            }
            else
            {
                _logger.LogInformation($"User role '{user.Role?.Name ?? "N/A"}' retrieved for user ID {userId}.");
            }
            return user?.Role?.Name;
        }

        // --- General User Write Operations ---
        public async Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            _logger.LogInformation($"Attempting to update user with ID: {userId}");
            var userToUpdate = await _context.Users
                                             .Include(u => u.Student)
                                             .Include(u => u.Teacher)
                                             .FirstOrDefaultAsync(u => u.Id == userId);
            if (userToUpdate == null)
            {
                _logger.LogWarning($"Update user failed: User with ID {userId} not found.");
                return false; // Should ideally be caught by NotFoundException if auth passes
            }

            // Check for username conflict if username is being changed
            if (!string.IsNullOrEmpty(request.Username) && userToUpdate.Username != request.Username)
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != userId))
                {
                    _logger.LogWarning($"Update user failed: Username '{request.Username}' is already taken by another user.");
                    throw new ConflictException($"Username '{request.Username}' is already taken.");
                }
                userToUpdate.Username = request.Username;
            }

            // Apply other updates
            if (!string.IsNullOrEmpty(request.FirstName)) userToUpdate.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName)) userToUpdate.LastName = request.LastName;
            if (request.Email != null) userToUpdate.Email = request.Email; // Allows setting email to null

            userToUpdate.UpdatedAt = DateTime.UtcNow;

            // Optional: Update associated profiles if the request DTO contains relevant fields
            // Example:
            // if (userToUpdate.Role?.Name == UserRoles.Student && userToUpdate.Student != null)
            // {
            //     // _mapper.Map(request, userToUpdate.Student); // Requires specific mapping for UpdateUserRequest to Student
            //     // userToUpdate.Student.UpdatedAt = DateTime.UtcNow;
            // }
            // else if (userToUpdate.Role?.Name == UserRoles.Teacher && userToUpdate.Teacher != null)
            // {
            //     // _mapper.Map(request, userToUpdate.Teacher); // Requires specific mapping for UpdateUserRequest to Teacher
            //     // userToUpdate.Teacher.UpdatedAt = DateTime.UtcNow;
            // }

            try
            {
                _context.Users.Update(userToUpdate); // Mark as modified
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User {userId} updated successfully.");
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency conflict occurred while updating user ID {userId}. Checking if user still exists...");
                if (!await _context.Users.AnyAsync(e => e.Id == userId))
                {
                    _logger.LogWarning($"User {userId} was deleted by another user during a concurrency conflict.");
                    throw new NotFoundException($"User with ID {userId} was deleted by another user.");
                }
                _logger.LogError(ex, $"Concurrency conflict occurred while updating user ID {userId}, but user still exists.");
                throw new ConflictException($"Concurrency conflict when updating user with ID {userId}. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during update of user ID {userId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while updating user with ID {userId}.", ex);
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            _logger.LogInformation($"Attempting to delete user with ID: {userId}");
            var userToDelete = await _context.Users
                                             .Include(u => u.Student)
                                                 .ThenInclude(s => s.Grades) // Include grades for student
                                             .Include(u => u.Student)
                                                 .ThenInclude(s => s.Attendances) // Include attendances for student
                                             .Include(u => u.Teacher)
                                                 .ThenInclude(t => t.TeacherSubjectGroupAssignments) // Include assignments for teacher
                                             .FirstOrDefaultAsync(u => u.Id == userId);

            if (userToDelete == null)
            {
                _logger.LogWarning($"Attempt to delete non-existent user with ID {userId}.");
                throw new NotFoundException($"User with ID {userId} not found.");
            }

            // --- Delete dependent entities before deleting the user ---
            if (userToDelete.Student != null)
            {
                _logger.LogInformation($"Removing associated student profile for user {userId}.");

                // Remove student's grades
                if (userToDelete.Student.Grades != null && userToDelete.Student.Grades.Any())
                {
                    _logger.LogInformation($"Removing {userToDelete.Student.Grades.Count} grades for student with UserId {userToDelete.Student.UserId}.");
                    _context.Grades.RemoveRange(userToDelete.Student.Grades);
                }

                // Remove student's attendances
                if (userToDelete.Student.Attendances != null && userToDelete.Student.Attendances.Any())
                {
                    _logger.LogInformation($"Removing {userToDelete.Student.Attendances.Count} attendance records for student with UserId {userToDelete.Student.UserId}.");
                    _context.Attendances.RemoveRange(userToDelete.Student.Attendances);
                }

                // Remove the student profile itself
                _context.Students.Remove(userToDelete.Student);
            }

            if (userToDelete.Teacher != null)
            {
                _logger.LogInformation($"Removing associated teacher profile for user {userId}.");

                // Check and potentially block deletion if teacher has assignments
                if (userToDelete.Teacher.TeacherSubjectGroupAssignments != null && userToDelete.Teacher.TeacherSubjectGroupAssignments.Any())
                {
                    _logger.LogWarning($"Cannot delete teacher {userToDelete.Teacher.TeacherId}: {userToDelete.Teacher.TeacherSubjectGroupAssignments.Count} associated assignments exist.");
                    throw new ConflictException($"Cannot delete teacher with ID {userToDelete.Teacher.TeacherId} because {userToDelete.Teacher.TeacherSubjectGroupAssignments.Count} associated assignments exist.");
                }

                // Check if teacher has grades they assigned (if Grade has a direct TeacherId)
                var hasGradesAssignedByTeacher = await _context.Grades.AnyAsync(g => g.TeacherId == userToDelete.Teacher.TeacherId);
                if (hasGradesAssignedByTeacher)
                {
                    _logger.LogWarning($"Cannot delete teacher {userToDelete.Teacher.TeacherId}: associated grades exist.");
                    throw new ConflictException($"Cannot delete teacher with ID {userToDelete.Teacher.TeacherId} because associated grades exist.");
                }

                // Remove the teacher profile itself
                _context.Teachers.Remove(userToDelete.Teacher);
            }

            // --- Remove the User object itself ---
            _context.Users.Remove(userToDelete);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User {userId} and all associated data deleted successfully.");
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DbUpdateException occurred while deleting user ID {userId}. Unhandled dependencies still exist. Details: {ex.InnerException?.Message ?? ex.Message}");
                throw new ConflictException($"Cannot delete user with ID {userId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during deletion of user ID {userId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while deleting user with ID {userId}: {ex.Message}", ex);
            }
        }

        // --- Profile Retrieval (for specific roles) ---
        // Note: GetUserByIdAsync already loads student/teacher profiles for UserDto mapping.
        // These methods specifically return the DTO of the profile itself.
        public async Task<StudentDto?> GetStudentByIdAsync(int userId)
        {
            var student = await _context.Students
                                        .Include(s => s.User)
                                        .Include(s => s.Group)
                                        .FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null)
            {
                _logger.LogWarning($"Student profile not found for user ID {userId}.");
            }
            else
            {
                _logger.LogInformation($"Student profile found for user ID {userId}.");
            }
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<TeacherDto?> GetTeacherByIdAsync(int userId)
        {
            var teacher = await _context.Teachers
                                        .Include(t => t.User)
                                        .FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null)
            {
                _logger.LogWarning($"Teacher profile not found for user ID {userId}.");
            }
            else
            {
                _logger.LogInformation($"Teacher profile found for user ID {userId}.");
            }
            return _mapper.Map<TeacherDto>(teacher);
        }
        // --- Group Management Methods (from your provided code) ---
        public async Task<GroupDto> AddGroupAsync(AddGroupRequest request)
        {
            _logger.LogInformation($"Attempting to add new group with name: {request.Name}");
            var existingGroup = await _context.Groups.AnyAsync(g => g.Name == request.Name);
            if (existingGroup)
            {
                _logger.LogWarning($"Group creation failed: Group with name '{request.Name}' already exists.");
                throw new ConflictException($"Group name '{request.Name}' already exists.");
            }
            var group = _mapper.Map<Group>(request);
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Group '{group.Name}' (ID: {group.GroupId}) added successfully.");
            return _mapper.Map<GroupDto>(group);
        }

        public async Task<List<GroupDto>> GetAllGroupsAsync()
        {
            var groups = await _context.Groups.ToListAsync();
            _logger.LogInformation($"{groups.Count} groups retrieved.");
            return _mapper.Map<List<GroupDto>>(groups);
        }

        public async Task<GroupDto?> GetGroupByIdAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                _logger.LogWarning($"Group with ID {groupId} not found.");
            }
            else
            {
                _logger.LogInformation($"Group '{group.Name}' (ID: {groupId}) retrieved.");
            }
            return _mapper.Map<GroupDto>(group);
        }

        public async Task UpdateGroupAsync(int groupId, UpdateGroupRequest request)
        {
            _logger.LogInformation($"Attempting to update group with ID: {groupId}");
            var groupToUpdate = await _context.Groups.FindAsync(groupId);
            if (groupToUpdate == null)
            {
                _logger.LogWarning($"Attempt to update non-existent group with ID {groupId}.");
                throw new NotFoundException($"Group with ID {groupId} not found.");
            }

            var conflictExists = await _context.Groups
                .AnyAsync(g => g.Name == request.Name && g.GroupId != groupId);
            if (conflictExists)
            {
                _logger.LogWarning($"Group update failed: Name '{request.Name}' already taken by another group.");
                throw new ConflictException($"Group name '{request.Name}' is already taken by another group.");
            }

            groupToUpdate.Name = request.Name; // Assuming only name is updated
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Group {groupId} updated successfully to name '{request.Name}'.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, $"Concurrency conflict during group {groupId} update: Checking if it still exists...");
                if (!_context.Groups.Any(e => e.GroupId == groupId))
                {
                    _logger.LogWarning($"Group {groupId} was deleted by another user during a concurrency conflict.");
                    throw new NotFoundException($"Group with ID {groupId} was deleted by another user.");
                }
                _logger.LogError(ex, $"Concurrency conflict during group {groupId} update, but group still exists.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while updating group {groupId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while updating group with ID {groupId}: {ex.Message}", ex);
            }
        }

        public async Task DeleteGroupAsync(int groupId)
        {
            _logger.LogInformation($"Attempting to delete group with ID: {groupId}");
            var groupToDelete = await _context.Groups
                                             .Include(g => g.Students)
                                             .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (groupToDelete == null)
            {
                _logger.LogWarning($"Attempt to delete non-existent group with ID {groupId}.");
                throw new NotFoundException($"Group with ID {groupId} not found.");
            }

            if (groupToDelete.Students != null && groupToDelete.Students.Any())
            {
                _logger.LogWarning($"Cannot delete group {groupId} ('{groupToDelete.Name}'): {groupToDelete.Students.Count} students are still assigned to it.");
                throw new ConflictException($"Cannot delete group '{groupToDelete.Name}' because {groupToDelete.Students.Count} students are still assigned to it.");
            }

            var hasAssignments = await _context.TeacherSubjectGroupAssignments.AnyAsync(tsga => tsga.GroupId == groupId);
            if (hasAssignments)
            {
                _logger.LogWarning($"Cannot delete group {groupId} ('{groupToDelete.Name}'): it has associated teacher subject group assignments.");
                throw new ConflictException($"Cannot delete group '{groupToDelete.Name}' because it has associated subject assignments.");
            }

            _context.Groups.Remove(groupToDelete);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Group {groupId} ('{groupToDelete.Name}') deleted successfully.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DbUpdateException occurred while deleting group {groupId}. Unhandled dependencies still exist. Details: {ex.InnerException?.Message ?? ex.Message}");
                throw new ConflictException($"Cannot delete group with ID {groupId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during deletion of group {groupId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while deleting group with ID {groupId}: {ex.Message}", ex);
            }
        }

        // --- Semester Management Methods (from your provided code) ---
        public async Task<SemesterDto> AddSemesterAsync(AddSemesterRequest request)
        {
            _logger.LogInformation($"Attempting to add new semester with name: {request.Name}");

            // ИСПРАВЛЕНИЕ: Убраны проверки .HasValue, так как StartDate и EndDate теперь не nullable в AddSemesterRequest
            if (request.EndDate <= request.StartDate)
            {
                _logger.LogWarning($"Semester creation failed: End date ({request.EndDate}) cannot be before or same as start date ({request.StartDate}).");
                throw new ArgumentException("Дата окончания не может быть раньше или такой же, как дата начала.");
            }

            // ИСПРАВЛЕНИЕ: Теперь проверяем имя и код
            if (await _context.Semesters.AnyAsync(s => s.Name == request.Name))
            {
                _logger.LogWarning($"Add semester failed: Semester with name '{request.Name}' already exists.");
                throw new ConflictException($"Семестр с названием '{request.Name}' уже существует.");
            }

            if (!string.IsNullOrWhiteSpace(request.Code) && await _context.Semesters.AnyAsync(s => s.Code == request.Code))
            {
                _logger.LogWarning($"Add semester failed: Semester with code '{request.Code}' already exists.");
                throw new ConflictException($"Семестр с кодом '{request.Code}' уже существует.");
            }

            var semester = _mapper.Map<Semester>(request);
            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Semester '{semester.Name}' (ID: {semester.SemesterId}) added successfully.");
            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task<IEnumerable<SemesterDto>> GetAllSemestersAsync()
        {
            var semesters = await _context.Semesters.ToListAsync();
            _logger.LogInformation($"{semesters.Count} semesters retrieved.");
            return _mapper.Map<IEnumerable<SemesterDto>>(semesters);
        }

        public async Task<SemesterDto?> GetSemesterByIdAsync(int semesterId)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);
            if (semester == null)
            {
                _logger.LogWarning($"Semester with ID {semesterId} not found.");
            }
            else
            {
                _logger.LogInformation($"Semester '{semester.Name}' (ID: {semesterId}) retrieved.");
            }
            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task UpdateSemesterAsync(int semesterId, UpdateSemesterRequest request)
        {
            _logger.LogInformation($"Attempting to update semester with ID: {semesterId}");
            var semesterToUpdate = await _context.Semesters.FindAsync(semesterId);

            if (semesterToUpdate == null)
            {
                _logger.LogWarning($"Attempt to update non-existent semester with ID {semesterId}.");
                throw new NotFoundException($"Semester with ID {semesterId} not found.");
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                if (request.EndDate.Value <= request.StartDate.Value)
                {
                    _logger.LogWarning($"Semester update failed: End date ({request.EndDate.Value}) cannot be before or same as start date ({request.StartDate.Value}).");
                    throw new ArgumentException("End date cannot be before or same as start date.");
                }
            }

            var conflictExists = await _context.Semesters
                .AnyAsync(s => s.Name == request.Name && s.SemesterId != semesterId);
            if (conflictExists)
            {
                _logger.LogWarning($"Semester update failed: Name '{request.Name}' already taken by another semester.");
                throw new ConflictException($"Semester name '{request.Name}' is already taken by another semester.");
            }

            semesterToUpdate.Name = request.Name;
            if (request.StartDate.HasValue) semesterToUpdate.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) semesterToUpdate.EndDate = request.EndDate.Value;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Semester {semesterId} ('{request.Name}') updated successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, $"Concurrency conflict during semester {semesterId} update. Checking if it still exists...");
                if (!await _context.Semesters.AnyAsync(e => e.SemesterId == semesterId))
                {
                    _logger.LogWarning($"Semester {semesterId} was deleted by another user during a concurrency conflict.");
                    throw new NotFoundException($"Semester with ID {semesterId} was deleted by another user.");
                }
                _logger.LogError(ex, $"Concurrency conflict during semester {semesterId} update, but semester still exists.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while updating semester {semesterId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while updating semester with ID {semesterId}: {ex.Message}", ex);
            }
        }

        public async Task DeleteSemesterAsync(int semesterId)
        {
            _logger.LogInformation($"Attempting to delete semester with ID: {semesterId}");
            var semesterToDelete = await _context.Semesters.FindAsync(semesterId);
            if (semesterToDelete == null)
            {
                _logger.LogWarning($"Attempt to delete non-existent semester with ID {semesterId}.");
                throw new NotFoundException($"Semester with ID {semesterId} not found.");
            }

            var hasAssignments = await _context.TeacherSubjectGroupAssignments.AnyAsync(tsga => tsga.SemesterId == semesterId);
            if (hasAssignments)
            {
                _logger.LogWarning($"Cannot delete semester {semesterId} ('{semesterToDelete.Name}'): it has associated teacher subject group assignments.");
                throw new ConflictException($"Cannot delete semester '{semesterToDelete.Name}' because it has associated subject assignments.");
            }

            _context.Semesters.Remove(semesterToDelete);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Semester {semesterId} ('{semesterToDelete.Name}') deleted successfully.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DbUpdateException occurred while deleting semester {semesterId}. Unhandled dependencies still exist. Details: {ex.InnerException?.Message ?? ex.Message}");
                throw new ConflictException($"Cannot delete semester with ID {semesterId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during deletion of semester {semesterId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while deleting semester with ID {semesterId}: {ex.Message}", ex);
            }
        }

        // --- Student Management Methods (from your provided code) ---
        public async Task<StudentDto?> AddStudentAsync(AddStudentRequest request)
        {
            _logger.LogInformation($"Attempting to add new student with username: {request.Username}");
            try
            {
                // 1. Check if the username already exists
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    _logger.LogWarning($"Student registration failed: Username '{request.Username}' is already taken.");
                    throw new ConflictException($"Username '{request.Username}' is already taken.");
                }

                // 2. Check if the specified GroupId exists and is valid
                // Если request.GroupId является int? (nullable), но в сущности Student.GroupId - int (не nullable),
                // то его нужно проверить на null и использовать .Value.
                if (!request.GroupId.HasValue) // Проверяем, что значение не равно null
                {
                    _logger.LogWarning($"Student registration failed: GroupId is required but was not provided.");
                    throw new BadRequestException("GroupId is required for student registration.");
                }

                // Проверяем существование группы по значению
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == request.GroupId.Value); // ИСПРАВЛЕНИЕ: используем .Value
                if (group == null)
                {
                    _logger.LogWarning($"Student registration failed: Group with ID {request.GroupId.Value} not found."); // ИСПРАВЛЕНИЕ: используем .Value
                    throw new NotFoundException($"Group with ID {request.GroupId.Value} not found."); // ИСПРАВЛЕНИЕ: используем .Value
                }

                // 3. Find RoleId for "Student"
                var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.Student);
                if (studentRole == null)
                {
                    _logger.LogError($"Critical error: Role '{UserRoles.Student}' not found in the database. Please seed roles.");
                    throw new InvalidOperationException($"Role '{UserRoles.Student}' not found in the database. Please seed roles.");
                }

                // 4. Create the User entity
                var newUser = _mapper.Map<User>(request); // Маппинг общих полей пользователя из запроса
                newUser.RoleId = studentRole.RoleId; // Присваиваем ID роли студента
                newUser.Role = studentRole; // Привязываем объект роли для навигационного свойства
                newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password); // Хеширование пароля
                newUser.CreatedAt = DateTime.UtcNow; // Установка метки времени создания
                newUser.UpdatedAt = DateTime.UtcNow; // Установка метки времени последнего обновления

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync(); // Сохраняем пользователя, чтобы получить сгенерированный Id

                // 5. Create the Student entity, linking it to the new User
                var newStudent = _mapper.Map<Student>(request); // Маппинг полей специфичных для студента из запроса
                newStudent.UserId = newUser.Id; // Связываем с только что созданным пользователем
                newStudent.GroupId = request.GroupId.Value; // ИСПРАВЛЕНИЕ: Явно получаем int значение из int? с помощью .Value
                newStudent.CreatedAt = DateTime.UtcNow; // Установка метки времени создания
                newStudent.UpdatedAt = DateTime.UtcNow; // Установка метки времени последнего обновления
                newStudent.IsActive = true; // Убедитесь, что это поле установлено, если оно необнуляемое в сущности Student

                _context.Students.Add(newStudent);
                await _context.SaveChangesAsync(); // Сохраняем профиль студента

                // 6. Load navigation properties for the DTO mapping
                await _context.Entry(newStudent).Reference(s => s.User).LoadAsync();
                await _context.Entry(newStudent).Reference(s => s.Group).LoadAsync();

                _logger.LogInformation($"Student '{request.Username}' (User ID: {newUser.Id}, Student ID: {newStudent.StudentId}) added successfully to group {request.GroupId.Value}."); // ИСПРАВЛЕНИЕ: используем .Value
                return _mapper.Map<StudentDto>(newStudent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while adding a new student with username: {request.Username}. Details: {ex.Message}");
                throw; // Повторно выбрасываем исключение после логирования
            }
        }

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _context.Students
                                         .Include(s => s.User)
                                         .Include(s => s.Group)
                                         .ToListAsync();
            _logger.LogInformation($"{students.Count} students retrieved.");
            return _mapper.Map<IEnumerable<StudentDto>>(students);
        }

        public async Task<StudentDto?> GetStudentDetailsByIdAsync(int studentId)
        {
            var student = await _context.Students
                                         .Include(s => s.User)
                                         .Include(s => s.Group)
                                         .FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                _logger.LogWarning($"Student with ID {studentId} not found.");
            }
            else
            {
                _logger.LogInformation($"Student '{student.User?.FirstName} {student.User?.LastName}' (ID: {studentId}) retrieved.");
            }
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<bool> UpdateStudentAsync(int studentId, UpdateStudentRequest request)
        {
            _logger.LogInformation($"Attempting to update student with ID: {studentId}");
            var studentToUpdate = await _context.Students
                                                .Include(s => s.User)
                                                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (studentToUpdate == null || studentToUpdate.User == null)
            {
                _logger.LogWarning($"Update student failed: Student or associated user with ID {studentId} not found.");
                throw new NotFoundException($"Student with ID {studentId} or associated user not found.");
            }

            // --- Update User properties ---
            if (!string.IsNullOrEmpty(request.Username) && studentToUpdate.User.Username != request.Username)
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != studentToUpdate.UserId))
                {
                    _logger.LogWarning($"Update student failed: Username '{request.Username}' is already taken by another user.");
                    throw new ConflictException($"Username '{request.Username}' is already taken.");
                }
                studentToUpdate.User.Username = request.Username;
            }

            if (!string.IsNullOrEmpty(request.FirstName)) studentToUpdate.User.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName)) studentToUpdate.User.LastName = request.LastName;
            if (request.Email != null) studentToUpdate.User.Email = request.Email;
            studentToUpdate.User.UpdatedAt = DateTime.UtcNow;

            // --- Update Student-specific properties ---
            if (request.DateOfBirth.HasValue) studentToUpdate.DateOfBirth = request.DateOfBirth.Value;
            if (request.EnrollmentDate.HasValue) studentToUpdate.EnrollmentDate = request.EnrollmentDate.Value;

            if (request.GroupId.HasValue && studentToUpdate.GroupId != request.GroupId.Value)
            {
                var newGroup = await _context.Groups.FindAsync(request.GroupId.Value);
                if (newGroup == null)
                {
                    _logger.LogWarning($"Update student failed: New GroupId {request.GroupId.Value} not found.");
                    throw new NotFoundException($"Group with ID {request.GroupId.Value} not found.");
                }
                studentToUpdate.GroupId = request.GroupId.Value;
                _logger.LogInformation($"Student {studentId} group updated to Group ID {request.GroupId.Value}.");
            }
            studentToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.Students.Update(studentToUpdate); // Explicitly mark as modified
                _context.Users.Update(studentToUpdate.User); // Explicitly mark associated User as modified
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Student {studentId} and associated user updated successfully.");
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency conflict occurred while updating student {studentId}.");
                if (!await _context.Students.AnyAsync(e => e.StudentId == studentId))
                {
                    _logger.LogWarning($"Student {studentId} was deleted by another user during a concurrency conflict.");
                    throw new NotFoundException($"Student with ID {studentId} was deleted by another user.");
                }
                throw new ConflictException($"Concurrency conflict when updating student with ID {studentId}. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during update of student {studentId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while updating student with ID {studentId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteStudentAsync(int studentId)
        {
            _logger.LogInformation($"Attempting to delete student with ID: {studentId}");
            var studentToDelete = await _context.Students
                                                .Include(s => s.User)
                                                .Include(s => s.Grades)
                                                .Include(s => s.Attendances)
                                                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (studentToDelete == null)
            {
                _logger.LogWarning($"Delete student failed: Student with ID {studentId} not found.");
                throw new NotFoundException($"Student with ID {studentId} not found.");
            }

            if (studentToDelete.Grades != null && studentToDelete.Grades.Any())
            {
                _logger.LogWarning($"Cannot delete student {studentId}: {studentToDelete.Grades.Count} associated grades exist.");
                throw new ConflictException($"Cannot delete student with ID {studentId} because {studentToDelete.Grades.Count} associated grades exist.");
            }

            if (studentToDelete.Attendances != null && studentToDelete.Attendances.Any())
            {
                _logger.LogWarning($"Cannot delete student {studentId}: {studentToDelete.Attendances.Count} associated attendance records exist.");
                throw new ConflictException($"Cannot delete student with ID {studentId} because {studentToDelete.Attendances.Count} associated attendance records exist.");
            }

            _context.Students.Remove(studentToDelete);
            if (studentToDelete.User != null)
            {
                _context.Users.Remove(studentToDelete.User); // Also delete the associated user
                _logger.LogInformation($"Associated user {studentToDelete.UserId} for student {studentId} marked for deletion.");
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Student {studentId} and associated user deleted successfully.");
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DbUpdateException occurred while deleting student {studentId}. Unhandled dependencies still exist. Details: {ex.InnerException?.Message ?? ex.Message}");
                throw new ConflictException($"Cannot delete student with ID {studentId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during deletion of student {studentId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while deleting student with ID {studentId}: {ex.Message}", ex);
            }
        }

        // --- Teacher Management Methods (from your provided code) ---
        // Note: This method is now effectively redundant with RegisterUserAsync if RegisterUserAsync
        // is the primary way to create teachers. However, it's kept here as you provided it.
        // The TeacherService.AddTeacherAsync method is the one called by the TeachersController.
        public async Task<TeacherDto?> AddTeacherAsync(AddTeacherRequest request)
        {
            _logger.LogInformation($"Attempting to add new teacher with username: {request.Username}");
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    _logger.LogWarning($"Teacher registration failed: Username '{request.Username}' is already taken.");
                    throw new ConflictException($"Username '{request.Username}' is already taken.");
                }

                var teacherRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.Teacher);
                if (teacherRole == null)
                {
                    _logger.LogError($"Critical error: Role '{UserRoles.Teacher}' not found in the database. Please seed roles.");
                    throw new InvalidOperationException($"Role '{UserRoles.Teacher}' not found in the database. Please seed roles.");
                }

                var newUser = _mapper.Map<User>(request);
                newUser.RoleId = teacherRole.RoleId;
                newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);
                newUser.CreatedAt = DateTime.UtcNow;
                newUser.UpdatedAt = DateTime.UtcNow;

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var newTeacher = _mapper.Map<Teacher>(request);
                newTeacher.UserId = newUser.Id;
                newTeacher.CreatedAt = DateTime.UtcNow;
                newTeacher.UpdatedAt = DateTime.UtcNow;

                _context.Teachers.Add(newTeacher);
                await _context.SaveChangesAsync();

                await _context.Entry(newTeacher).Reference(t => t.User).LoadAsync();
                if (newTeacher.User != null)
                {
                    await _context.Entry(newTeacher.User).Reference(u => u.Role).LoadAsync();
                }

                _logger.LogInformation($"Teacher '{request.Username}' (User ID: {newUser.Id}, Teacher ID: {newTeacher.TeacherId}) added successfully.");
                return _mapper.Map<TeacherDto>(newTeacher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while adding a new teacher with username: {request.Username}. Details: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<TeacherDto>> GetAllTeachersAsync()
        {
            var teachers = await _context.Teachers
                                         .Include(t => t.User)
                                         .ToListAsync();
            _logger.LogInformation($"{teachers.Count} teachers retrieved.");
            return _mapper.Map<IEnumerable<TeacherDto>>(teachers);
        }

        public async Task<TeacherDto?> GetTeacherDetailsByIdAsync(int teacherId)
        {
            var teacher = await _context.Teachers
                                        .Include(t => t.User)
                                        .FirstOrDefaultAsync(t => t.TeacherId == teacherId);
            if (teacher == null)
            {
                _logger.LogWarning($"Teacher with ID {teacherId} not found.");
            }
            else
            {
                _logger.LogInformation($"Teacher '{teacher.User?.FirstName} {teacher.User?.LastName}' (ID: {teacherId}) retrieved.");
            }
            return _mapper.Map<TeacherDto>(teacher);
        }
        // Note: This method is now effectively redundant with the TeacherService.UpdateTeacherAsync
        // It's kept here as you provided it, but typically only one service would handle updates for a profile type.
        public async Task<bool> UpdateTeacherAsync(int teacherId, UpdateTeacherRequest request)
        {
            _logger.LogInformation($"Attempting to update teacher with ID: {teacherId}");
            var teacherToUpdate = await _context.Teachers
                                                 .Include(t => t.User)
                                                 .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            if (teacherToUpdate == null || teacherToUpdate.User == null)
            {
                _logger.LogWarning($"Update teacher failed: Teacher or associated user with ID {teacherId} not found.");
                throw new NotFoundException($"Teacher with ID {teacherId} or associated user not found.");
            }

            if (!string.IsNullOrEmpty(request.Username) && teacherToUpdate.User.Username != request.Username)
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != teacherToUpdate.UserId))
                {
                    _logger.LogWarning($"Update teacher failed: Username '{request.Username}' is already taken by another user.");
                    throw new ConflictException($"Username '{request.Username}' is already taken.");
                }
                teacherToUpdate.User.Username = request.Username;
            }

            if (!string.IsNullOrEmpty(request.FirstName)) teacherToUpdate.User.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName)) teacherToUpdate.User.LastName = request.LastName;
            if (request.Email != null) teacherToUpdate.User.Email = request.Email;
            teacherToUpdate.User.UpdatedAt = DateTime.UtcNow;

            if (request.Department != null) teacherToUpdate.Department = request.Department;
            if (request.Position != null) teacherToUpdate.Position = request.Position;
            teacherToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.Teachers.Update(teacherToUpdate);
                _context.Users.Update(teacherToUpdate.User);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Teacher {teacherId} and associated user updated successfully.");
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency conflict occurred while updating teacher {teacherId}.");
                if (!await _context.Teachers.AnyAsync(e => e.TeacherId == teacherId))
                {
                    _logger.LogWarning($"Teacher {teacherId} was deleted by another user during a concurrency conflict.");
                    throw new NotFoundException($"Teacher with ID {teacherId} was deleted by another user.");
                }
                throw new ConflictException($"Concurrency conflict when updating teacher with ID {teacherId}. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during update of teacher {teacherId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while updating teacher with ID {teacherId}: {ex.Message}", ex);
            }
        }

        // Note: This method is now effectively redundant with the TeacherService.DeleteTeacherAsync
        // It's kept here as you provided it, but typically only one service would handle deletions for a profile type.
        public async Task<bool> DeleteTeacherAsync(int teacherId)
        {
            _logger.LogInformation($"Attempting to delete teacher with ID: {teacherId}");
            var teacherToDelete = await _context.Teachers
                                                 .Include(t => t.User)
                                                 .Include(t => t.TeacherSubjectGroupAssignments)
                                                 .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            if (teacherToDelete == null)
            {
                _logger.LogWarning($"Delete teacher failed: Teacher with ID {teacherId} not found.");
                throw new NotFoundException($"Teacher with ID {teacherId} not found.");
            }

            if (teacherToDelete.TeacherSubjectGroupAssignments != null && teacherToDelete.TeacherSubjectGroupAssignments.Any())
            {
                _logger.LogWarning($"Cannot delete teacher {teacherId}: {teacherToDelete.TeacherSubjectGroupAssignments.Count} associated assignments exist.");
                throw new ConflictException($"Cannot delete teacher with ID {teacherId} because {teacherToDelete.TeacherSubjectGroupAssignments.Count} associated assignments exist.");
            }
            var hasGrades = await _context.Grades.AnyAsync(g => g.TeacherId == teacherId);
            if (hasGrades)
            {
                _logger.LogWarning($"Cannot delete teacher {teacherId}: associated grades exist.");
                throw new ConflictException($"Cannot delete teacher with ID {teacherId} because associated grades exist.");
            }

            _context.Teachers.Remove(teacherToDelete);
            if (teacherToDelete.User != null)
            {
                _context.Users.Remove(teacherToDelete.User);
                _logger.LogInformation($"Associated user {teacherToDelete.UserId} for teacher {teacherId} marked for deletion.");
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Teacher {teacherId} and associated user deleted successfully.");
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DbUpdateException occurred while deleting teacher {teacherId}. Unhandled dependencies still exist. Details: {ex.InnerException?.Message ?? ex.Message}");
                throw new ConflictException($"Cannot delete teacher with ID {teacherId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during deletion of teacher {teacherId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while deleting teacher with ID {teacherId}: {ex.Message}", ex);
            }
        }

        // --- Subject Management Methods (from your provided code) ---
        public async Task<SubjectDto> AddSubjectAsync(AddSubjectRequest request)
        {
            _logger.LogInformation($"Attempting to add new subject with name: {request.Name}");
            try
            {
                var existingSubject = await _context.Subjects.AnyAsync(s => s.Name == request.Name);
                if (existingSubject)
                {
                    _logger.LogWarning($"Subject creation failed: Subject with name '{request.Name}' already exists.");
                    throw new ConflictException($"Subject with name '{request.Name}' already exists.");
                }

                var subject = _mapper.Map<Subject>(request);
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Subject '{subject.Name}' (ID: {subject.SubjectId}) added successfully.");
                return _mapper.Map<SubjectDto>(subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while adding new subject with name: {request.Name}. Details: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync()
        {
            var subjects = await _context.Subjects.ToListAsync();
            _logger.LogInformation($"{subjects.Count} subjects retrieved.");
            return _mapper.Map<IEnumerable<SubjectDto>>(subjects);
        }

        public async Task<SubjectDto?> GetSubjectByIdAsync(int subjectId)
        {
            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null)
            {
                _logger.LogWarning($"Subject with ID {subjectId} not found.");
            }
            else
            {
                _logger.LogInformation($"Subject '{subject.Name}' (ID: {subjectId}) retrieved.");
            }
            return _mapper.Map<SubjectDto>(subject);
        }

        public async Task<bool> UpdateSubjectAsync(int subjectId, UpdateSubjectRequest request)
        {
            _logger.LogInformation($"Attempting to update subject with ID: {subjectId}");
            var subjectToUpdate = await _context.Subjects.FindAsync(subjectId);

            if (subjectToUpdate == null)
            {
                _logger.LogWarning($"Update subject failed: Subject with ID {subjectId} not found.");
                throw new NotFoundException($"Subject with ID {subjectId} not found.");
            }

            var conflictExists = await _context.Subjects
                .AnyAsync(s => s.Name == request.Name && s.SubjectId != subjectId);
            if (conflictExists)
            {
                _logger.LogWarning($"Update subject failed: Name '{request.Name}' is already taken by another subject.");
                throw new ConflictException($"Subject name '{request.Name}' is already taken by another subject.");
            }

            subjectToUpdate.Name = request.Name;
            // Map other fields from request to subjectToUpdate if any
            subjectToUpdate.Code = request.Code;
            subjectToUpdate.Description = request.Description;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Subject {subjectId} ('{request.Name}') updated successfully.");
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency conflict occurred while updating subject {subjectId}. Checking if it still exists...");
                if (!await _context.Subjects.AnyAsync(e => e.SubjectId == subjectId))
                {
                    _logger.LogWarning($"Subject {subjectId} was deleted by another user during a concurrency conflict.");
                    throw new NotFoundException($"Subject with ID {subjectId} was deleted by another user.");
                }
                _logger.LogError(ex, $"Concurrency conflict occurred while updating subject {subjectId}, but subject still exists.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while updating subject {subjectId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while updating subject with ID {subjectId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteSubjectAsync(int subjectId)
        {
            _logger.LogInformation($"Attempting to delete subject with ID: {subjectId}");
            var subjectToDelete = await _context.Subjects.FindAsync(subjectId);
            if (subjectToDelete == null)
            {
                _logger.LogWarning($"Delete subject failed: Subject with ID {subjectId} not found.");
                throw new NotFoundException($"Subject with ID {subjectId} not found.");
            }

            var hasAssignments = await _context.TeacherSubjectGroupAssignments.AnyAsync(tsga => tsga.SubjectId == subjectId);
            if (hasAssignments)
            {
                _logger.LogWarning($"Cannot delete subject {subjectId} ('{subjectToDelete.Name}'): it has associated teacher subject group assignments.");
                throw new ConflictException($"Cannot delete subject '{subjectToDelete.Name}' because it has associated assignments.");
            }
            var hasGrades = await _context.Grades.AnyAsync(g => g.SubjectId == subjectId);
            if (hasGrades)
            {
                _logger.LogWarning($"Cannot delete subject {subjectId} ('{subjectToDelete.Name}'): it has associated grades.");
                throw new ConflictException($"Cannot delete subject '{subjectToDelete.Name}' because it has associated grades.");
            }

            _context.Subjects.Remove(subjectToDelete);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Subject {subjectId} ('{subjectToDelete.Name}') deleted successfully.");
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DbUpdateException occurred while deleting subject {subjectId}. Unhandled dependencies still exist. Details: {ex.InnerException?.Message ?? ex.Message}");
                throw new ConflictException($"Cannot delete subject with ID {subjectId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during deletion of subject {subjectId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while deleting subject with ID {subjectId}: {ex.Message}", ex);
            }
        }

        // --- Assignment Management Methods (from your provided code) ---
        public async Task<TeacherSubjectGroupAssignmentDto> AddAssignmentAsync(AddTeacherSubjectGroupAssignmentRequest request)
        {
            _logger.LogInformation($"Attempting to add new assignment for Teacher ID {request.TeacherId}, Subject ID {request.SubjectId}, Group ID {request.GroupId}, Semester ID {request.SemesterId}.");
            try
            {
                var existingAssignment = await _context.TeacherSubjectGroupAssignments
                    .AnyAsync(tsga => tsga.TeacherId == request.TeacherId &&
                                     tsga.SubjectId == request.SubjectId &&
                                     tsga.GroupId == request.GroupId &&
                                     tsga.SemesterId == request.SemesterId);

                if (existingAssignment)
                {
                    _logger.LogWarning($"Assignment creation failed: An identical assignment already exists for Teacher ID {request.TeacherId}, Subject ID {request.SubjectId}, Group ID {request.GroupId}, Semester ID {request.SemesterId}.");
                    throw new ConflictException("An identical assignment already exists.");
                }

                var teacher = await _context.Teachers.FindAsync(request.TeacherId);
                var subject = await _context.Subjects.FindAsync(request.SubjectId);
                var group = await _context.Groups.FindAsync(request.GroupId);
                var semester = await _context.Semesters.FindAsync(request.SemesterId);

                if (teacher == null) throw new NotFoundException($"Teacher with ID {request.TeacherId} not found.");
                if (subject == null) throw new NotFoundException($"Subject with ID {request.SubjectId} not found.");
                if (group == null) throw new NotFoundException($"Group with ID {request.GroupId} not found.");
                if (semester == null) throw new NotFoundException($"Semester with ID {request.SemesterId} not found.");

                var assignment = _mapper.Map<TeacherSubjectGroupAssignment>(request);
                _context.TeacherSubjectGroupAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // Load navigation properties for the DTO mapping
                await _context.Entry(assignment).Reference(tsga => tsga.Teacher).LoadAsync();
                await _context.Entry(assignment).Reference(tsga => tsga.Subject).LoadAsync();
                await _context.Entry(assignment).Reference(tsga => tsga.Group).LoadAsync();
                await _context.Entry(assignment).Reference(tsga => tsga.Semester).LoadAsync();
                if (assignment.Teacher != null)
                {
                    await _context.Entry(assignment.Teacher).Reference(t => t.User).LoadAsync();
                }

                _logger.LogInformation($"Assignment (ID: {assignment.TeacherSubjectGroupAssignmentId}) added successfully for Teacher {request.TeacherId}, Subject {request.SubjectId}, Group {request.GroupId}, Semester {request.SemesterId}.");
                return _mapper.Map<TeacherSubjectGroupAssignmentDto>(assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while adding a new assignment for Teacher ID {request.TeacherId}, Subject ID {request.SubjectId}, Group ID {request.GroupId}, Semester ID {request.SemesterId}. Details: {ex.Message}");
                throw;
            }
        }

        public async Task<TeacherSubjectGroupAssignmentDto?> GetAssignmentByIdAsync(int assignmentId)
        {
            var assignment = await _context.TeacherSubjectGroupAssignments
                                           .Include(tsga => tsga.Teacher)
                                               .ThenInclude(t => t.User)
                                           .Include(tsga => tsga.Subject)
                                           .Include(tsga => tsga.Group)
                                           .Include(tsga => tsga.Semester)
                                           .FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == assignmentId);
            if (assignment == null)
            {
                _logger.LogWarning($"Assignment with ID {assignmentId} not found.");
            }
            else
            {
                _logger.LogInformation($"Assignment {assignmentId} retrieved.");
            }
            return _mapper.Map<TeacherSubjectGroupAssignmentDto>(assignment);
        }

        public async Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAllAssignmentsAsync()
        {
            var assignments = await _context.TeacherSubjectGroupAssignments
                                            .Include(tsga => tsga.Teacher)
                                                .ThenInclude(t => t.User)
                                            .Include(tsga => tsga.Subject)
                                            .Include(tsga => tsga.Group)
                                            .Include(tsga => tsga.Semester)
                                            .ToListAsync();
            _logger.LogInformation($"{assignments.Count} assignments retrieved.");
            return _mapper.Map<IEnumerable<TeacherSubjectGroupAssignmentDto>>(assignments);
        }

        public async Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAssignmentsByTeacherIdAsync(int teacherId)
        {
            var assignments = await _context.TeacherSubjectGroupAssignments
                .Where(tsga => tsga.TeacherId == teacherId)
                .Include(tsga => tsga.Teacher)
                    .ThenInclude(t => t.User)
                .Include(tsga => tsga.Subject)
                .Include(tsga => tsga.Group)
                .Include(tsga => tsga.Semester)
                .ToListAsync();

            if (!assignments.Any())
            {
                _logger.LogInformation($"No assignments found for teacher ID {teacherId}.");
            }
            else
            {
                _logger.LogInformation($"{assignments.Count} assignments found for teacher ID {teacherId}.");
            }
            return _mapper.Map<IEnumerable<TeacherSubjectGroupAssignmentDto>>(assignments);
        }

        public async Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateTeacherSubjectGroupAssignmentRequest request)
        {
            _logger.LogInformation($"Attempting to update assignment with ID: {assignmentId}");
            var assignmentToUpdate = await _context.TeacherSubjectGroupAssignments.FindAsync(assignmentId);

            if (assignmentToUpdate == null)
            {
                _logger.LogWarning($"Update assignment failed: Assignment with ID {assignmentId} not found.");
                throw new NotFoundException($"Assignment with ID {assignmentId} not found.");
            }

            var conflictExists = await _context.TeacherSubjectGroupAssignments
                .AnyAsync(tsga => tsga.TeacherId == request.TeacherId &&
                                 tsga.SubjectId == request.SubjectId &&
                                 tsga.GroupId == request.GroupId &&
                                 tsga.SemesterId == request.SemesterId &&
                                 tsga.TeacherSubjectGroupAssignmentId != assignmentId);

            if (conflictExists)
            {
                _logger.LogWarning($"Update assignment failed: An identical assignment already exists for Teacher ID {request.TeacherId}, Subject ID {request.SubjectId}, Group ID {request.GroupId}, Semester ID {request.SemesterId}.");
                throw new ConflictException("An identical assignment already exists.");
            }

            var teacher = await _context.Teachers.FindAsync(request.TeacherId);
            var subject = await _context.Subjects.FindAsync(request.SubjectId);
            var group = await _context.Groups.FindAsync(request.GroupId);
            var semester = await _context.Semesters.FindAsync(request.SemesterId);

            if (teacher == null) throw new NotFoundException($"Teacher with ID {request.TeacherId} not found.");
            if (subject == null) throw new NotFoundException($"Subject with ID {request.SubjectId} not found.");
            if (group == null) throw new NotFoundException($"Group with ID {request.GroupId} not found.");
            if (semester == null) throw new NotFoundException($"Semester with ID {request.SemesterId} not found.");

            _mapper.Map(request, assignmentToUpdate); // Map changes from DTO to entity

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Assignment {assignmentId} updated successfully.");
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency conflict occurred while updating assignment {assignmentId}. Checking if it still exists...");
                if (!await _context.TeacherSubjectGroupAssignments.AnyAsync(e => e.TeacherSubjectGroupAssignmentId == assignmentId))
                {
                    _logger.LogWarning($"Assignment {assignmentId} was deleted by another user during a concurrency conflict.");
                    throw new NotFoundException($"Assignment with ID {assignmentId} was deleted by another user.");
                }
                _logger.LogError(ex, $"Concurrency conflict occurred while updating assignment {assignmentId}, but assignment still exists.");
                throw new ConflictException($"Concurrency conflict when updating assignment with ID {assignmentId}. Please try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during update of assignment {assignmentId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while updating assignment with ID {assignmentId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAssignmentAsync(int assignmentId)
        {
            _logger.LogInformation($"Attempting to delete assignment with ID: {assignmentId}");
            var assignmentToDelete = await _context.TeacherSubjectGroupAssignments.FindAsync(assignmentId);

            if (assignmentToDelete == null)
            {
                _logger.LogWarning($"Delete assignment failed: Assignment with ID {assignmentId} not found.");
                throw new NotFoundException($"Assignment with ID {assignmentId} not found.");
            }

            var hasGrades = await _context.Grades.AnyAsync(g => g.TeacherSubjectGroupAssignmentId == assignmentId);
            if (hasGrades)
            {
                _logger.LogWarning($"Cannot delete assignment {assignmentId}: it has associated grades.");
                throw new ConflictException($"Cannot delete assignment with ID {assignmentId} because it has associated grades.");
            }

            _context.TeacherSubjectGroupAssignments.Remove(assignmentToDelete);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Assignment {assignmentId} deleted successfully.");
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DbUpdateException occurred while deleting assignment {assignmentId}. Unhandled dependencies still exist. Details: {ex.InnerException?.Message ?? ex.Message}");
                throw new ConflictException($"Cannot delete assignment with ID {assignmentId} due to existing related data. Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during deletion of assignment {assignmentId}. Details: {ex.Message}");
                throw new Exception($"An unexpected error occurred while deleting assignment with ID {assignmentId}: {ex.Message}", ex);
            }
        }

        // --- Authorization Methods ---

        private async Task<bool> IsAdministrator(int userId)
        {
            return await GetUserRoleAsync(userId) == UserRoles.Administrator;
        }

        private async Task<bool> IsTeacher(int userId)
        {
            return await GetUserRoleAsync(userId) == UserRoles.Teacher;
        }

        private async Task<bool> IsStudent(int userId)
        {
            return await GetUserRoleAsync(userId) == UserRoles.Student;
        }

        // --- User-Related Authorization Methods ---
        public async Task<bool> CanUserViewAllUsersAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to view all users: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewUserDetailsAsync(int currentUserId, int targetUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {currentUserRole ?? "N/A"}) attempting to view user details for user {targetUserId}.");

            if (currentUserRole == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can view any user details.");
                return true;
            }

            // Users can view their own details
            if (currentUserId == targetUserId)
            {
                _logger.LogInformation($"Authorization granted: User {currentUserId} can view their own details.");
                return true;
            }

            // Teachers can view details of other users for practical reasons (e.g., in a class list)
            // If you want to restrict this, remove this block.
            if (currentUserRole == UserRoles.Teacher)
            {
                _logger.LogInformation($"Authorization granted: Teacher user {currentUserId} can view any user details.");
                return true;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {currentUserRole ?? "N/A"}) cannot view user details for user {targetUserId}.");
            return false;
        }

        public async Task<bool> CanUserUpdateUserAsync(int currentUserId, int targetUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {currentUserRole ?? "N/A"}) attempting to update user {targetUserId}.");

            if (currentUserRole == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can update any user.");
                return true;
            }

            if (currentUserId == targetUserId)
            {
                _logger.LogInformation($"Authorization granted: User {currentUserId} can update their own profile.");
                return true;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {currentUserRole ?? "N/A"}) cannot update user {targetUserId}.");
            return false;
        }

        public async Task<bool> CanUserManageUsersAsync(int currentUserId)
        {
            // Только администраторы могут управлять пользователями
            return await IsUserAdmin(currentUserId);
        }

        public async Task<bool> CanUserDeleteUserAsync(int currentUserId, int targetUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to delete user {targetUserId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized; // Only administrators can delete users
        }
        // --- Student-Related Authorization Methods ---
        public async Task<bool> CanUserAddStudentAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to add student: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewAllStudentsAsync(int currentUserId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            bool authorized = role == UserRoles.Administrator || role == UserRoles.Teacher;
            _logger.LogInformation($"User {currentUserId} authorization to view all students: {authorized} (Role: {role ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewStudentDetailsAsync(int currentUserId, int studentId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {currentUserRole ?? "N/A"}) attempting to view student details for Student ID {studentId}.");

            if (currentUserRole == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can view any student details.");
                return true;
            }

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                _logger.LogWarning($"Authorization failed: Student with ID {studentId} not found for viewing.");
                throw new NotFoundException($"Student with ID {studentId} not found.");
            }

            if (currentUserRole == UserRoles.Student)
            {
                bool isOwnStudent = student.UserId == currentUserId;
                if (isOwnStudent)
                {
                    _logger.LogInformation($"Authorization granted: Student (User ID: {currentUserId}) can view their own Student ID {studentId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Student (User ID: {currentUserId}) cannot view Student ID {studentId} (not their own).");
                }
                return isOwnStudent;
            }

            if (currentUserRole == UserRoles.Teacher)
            {
                // Предполагается, что GetTeacherByIdAsync возвращает TeacherDto, а не сам Teacher.
                // Также предполагается, что TeacherDto содержит TeacherId.
                var teacherProfile = await GetTeacherByIdAsync(currentUserId); // Вызов из UserService
                if (teacherProfile == null || teacherProfile.TeacherId == null) // Проверка на null и TeacherId
                {
                    _logger.LogWarning($"Authorization failed: Teacher profile or TeacherId not found for user {currentUserId}.");
                    return false;
                }

                // ИСПРАВЛЕНИЕ: Удаляем .HasValue, так как student.GroupId является int
                // ИСПРАВЛЕНИЕ: Используем student.GroupId напрямую, без .Value
                // Добавляем проверку, что GroupId студента валиден (не 0, если 0 - это "неназначенная группа")
                if (student.GroupId > 0) // Если GroupId является обязательным и > 0 указывает на назначенную группу
                {
                    bool teachesStudentGroup = await _context.TeacherSubjectGroupAssignments
                        .AnyAsync(tsga => tsga.TeacherId == teacherProfile.TeacherId &&
                                         tsga.GroupId == student.GroupId); // ИСПРАВЛЕНИЕ: Используем student.GroupId напрямую

                    if (teachesStudentGroup)
                    {
                        _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) teaches student {studentId}'s group {student.GroupId}.");
                    }
                    else
                    {
                        _logger.LogWarning($"Authorization failed: Teacher (User ID: {currentUserId}) does not teach student {studentId}'s group {student.GroupId}.");
                    }
                    return teachesStudentGroup;
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Student {studentId} has an invalid or unassigned group ID ({student.GroupId}), teacher cannot view.");
                    return false; // Студент не назначен в группу, поэтому преподаватель не может его просматривать таким образом.
                }
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {currentUserRole ?? "N/A"}) cannot view student details for Student ID {studentId}.");
            return false;
        }

        public async Task<bool> CanUserUpdateStudentAsync(int currentUserId, int studentId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to update student {studentId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserDeleteStudentAsync(int currentUserId, int studentId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to delete student {studentId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        // --- Teacher-Related Authorization Methods ---
        public async Task<bool> CanUserAddTeacherAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to add teacher: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewAllTeachersAsync(int currentUserId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            bool authorized = role == UserRoles.Administrator || role == UserRoles.Teacher;
            _logger.LogInformation($"User {currentUserId} authorization to view all teachers: {authorized} (Role: {role ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewTeacherDetailsAsync(int currentUserId, int teacherId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {currentUserRole ?? "N/A"}) attempting to view teacher details for Teacher ID {teacherId}.");

            if (currentUserRole == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can view any teacher details.");
                return true;
            }

            // Teachers can view other teachers' profiles (e.g., for collaboration)
            if (currentUserRole == UserRoles.Teacher)
            {
                // A teacher can always view their own profile.
                var teacherProfile = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.TeacherId == teacherId);
                if (teacherProfile != null && teacherProfile.UserId == currentUserId)
                {
                    _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) can view their own profile.");
                    return true;
                }
                // Also, a teacher can view *other* teachers' profiles for collaboration/information.
                _logger.LogInformation($"Authorization granted: Teacher user {currentUserId} can view any teacher details (as per collaboration rule).");
                return true;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {currentUserRole ?? "N/A"}) cannot view teacher details for Teacher ID {teacherId}.");
            return false;
        }

        public async Task<bool> CanUserUpdateTeacherAsync(int currentUserId, int teacherId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} authorization to update teacher {teacherId}: {currentUserRole ?? "N/A"}.");

            if (currentUserRole == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can update any teacher.");
                return true;
            }
            // If a teacher should only update their own profile:
            var teacherProfile = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.TeacherId == teacherId);
            if (teacherProfile != null && teacherProfile.UserId == currentUserId && currentUserRole == UserRoles.Teacher)
            {
                _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) can update their own profile.");
                return true;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {currentUserRole ?? "N/A"}) cannot update teacher {teacherId}.");
            return false; // Only administrators can update any teacher (or specific teacher can update themselves if added above)
        }

        public async Task<bool> CanUserDeleteTeacherAsync(int currentUserId, int teacherId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to delete teacher {teacherId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized; // Only administrators can delete teachers
        }

        // --- Subject-Related Authorization Methods ---
        public async Task<bool> CanUserAddSubjectAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to add subject: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewAllSubjectsAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator ||
                             currentUserRole == UserRoles.Teacher ||
                             currentUserRole == UserRoles.Student;
            _logger.LogInformation($"User {currentUserId} authorization to view all subjects: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewSubjectDetailsAsync(int currentUserId, int subjectId)
        {
            var userRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {userRole ?? "N/A"}) attempting to view subject details for Subject ID {subjectId}.");

            if (string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning($"Authorization failed: User {currentUserId} not found or no role assigned.");
                return false;
            }

            if (userRole == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can view any subject details.");
                return true;
            }
            else if (userRole == UserRoles.Teacher)
            {
                bool isTeacherAssignedToSubject = await _context.TeacherSubjectGroupAssignments
                                                                 .AnyAsync(tsga => tsga.Teacher.UserId == currentUserId &&
                                                                                   tsga.SubjectId == subjectId);
                if (isTeacherAssignedToSubject)
                {
                    _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) is assigned to Subject ID {subjectId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Teacher (User ID: {currentUserId}) is not assigned to Subject ID {subjectId}.");
                }
                return isTeacherAssignedToSubject;
            }
            else if (userRole == UserRoles.Student)
            {
                // A student can view subjects they are enrolled in or have grades/attendance for.
                bool isStudentAssociatedWithSubject = await _context.Students
                    .Where(s => s.UserId == currentUserId)
                    .AnyAsync(s => s.Group != null && s.Group.TeacherSubjectGroupAssignments.Any(tsga => tsga.SubjectId == subjectId) ||
                                   s.Grades.Any(g => g.SubjectId == subjectId) ||
                                   s.Attendances.Any(a => a.TeacherSubjectGroupAssignment.SubjectId == subjectId));

                if (isStudentAssociatedWithSubject)
                {
                    _logger.LogInformation($"Authorization granted: Student (User ID: {currentUserId}) is associated with Subject ID {subjectId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Student (User ID: {currentUserId}) is not associated with Subject ID {subjectId}.");
                }
                return isStudentAssociatedWithSubject;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {userRole ?? "N/A"}) is not allowed to view subject details.");
            return false;
        }

        public async Task<bool> CanUserUpdateSubjectAsync(int currentUserId, int subjectId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to update subject {subjectId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserDeleteSubjectAsync(int currentUserId, int subjectId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to delete subject {subjectId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        // --- Group-Related Authorization Methods ---
        public async Task<bool> CanUserAddGroupAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to add group: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewAllGroupsAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator || currentUserRole == UserRoles.Teacher;
            _logger.LogInformation($"User {currentUserId} authorization to view all groups: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewGroupDetailsAsync(int currentUserId, int groupId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {role ?? "N/A"}) attempting to view group details for Group ID {groupId}.");

            if (role == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can view any group details.");
                return true;
            }

            if (role == UserRoles.Teacher)
            {
                var teacherProfile = await _context.Teachers
                                                     .Include(t => t.TeacherSubjectGroupAssignments)
                                                     .FirstOrDefaultAsync(t => t.UserId == currentUserId);
                if (teacherProfile == null)
                {
                    _logger.LogWarning($"Authorization failed: Teacher profile not found for user {currentUserId}.");
                    return false;
                }
                bool isAssignedToGroup = teacherProfile.TeacherSubjectGroupAssignments.Any(tsga => tsga.GroupId == groupId);
                if (isAssignedToGroup)
                {
                    _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) is assigned to Group ID {groupId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Teacher (User ID: {currentUserId}) is not assigned to Group ID {groupId}.");
                }
                return isAssignedToGroup;
            }

            if (role == UserRoles.Student)
            {
                var studentProfile = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                if (studentProfile == null)
                {
                    _logger.LogWarning($"Authorization failed: Student profile not found for user {currentUserId}.");
                    return false;
                }
                bool isOwnGroup = studentProfile.GroupId == groupId;
                if (isOwnGroup)
                {
                    _logger.LogInformation($"Authorization granted: Student (User ID: {currentUserId}) can view their own Group ID {groupId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Student (User ID: {currentUserId}) cannot view Group ID {groupId} (not their own group).");
                }
                return isOwnGroup;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {role ?? "N/A"}) cannot view group details.");
            return false;
        }

        public async Task<bool> CanUserUpdateGroupAsync(int currentUserId, int groupId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to update group {groupId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserDeleteGroupAsync(int currentUserId, int groupId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to delete group {groupId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        // --- Semester-Related Authorization Methods ---
        public async Task<bool> CanUserAddSemesterAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to add semester: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewAllSemestersAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator || currentUserRole == UserRoles.Teacher || currentUserRole == UserRoles.Student;
            _logger.LogInformation($"User {currentUserId} authorization to view all semesters: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewSemesterDetailsAsync(int currentUserId, int semesterId)
        {
            var userRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {userRole ?? "N/A"}) attempting to view semester details for Semester ID {semesterId}.");

            if (string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning($"Authorization failed: User {currentUserId} not found or no role assigned.");
                return false;
            }

            if (userRole == UserRoles.Administrator || userRole == UserRoles.Teacher)
            {
                _logger.LogInformation($"Authorization granted: User {currentUserId} (Role: {userRole ?? "N/A"}) can view any semester details.");
                return true;
            }
            else if (userRole == UserRoles.Student)
            {
                // Simple rule for now: any student can view semester details.
                // More complex logic: check if student is in a group assigned to this semester.
                _logger.LogInformation($"Authorization granted: Student (User ID: {currentUserId}) can view any semester details.");
                return true;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {userRole ?? "N/A"}) is not allowed to view semester details.");
            return false;
        }

        public async Task<bool> CanUserUpdateSemesterAsync(int currentUserId, int semesterId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to update semester {semesterId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserDeleteSemesterAsync(int currentUserId, int semesterId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to delete semester {semesterId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        // --- Assignment-Related Authorization Methods ---
        public async Task<bool> CanUserAddAssignmentAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator || currentUserRole == UserRoles.Teacher; // Admins and Teachers can add assignments
            _logger.LogInformation($"User {currentUserId} authorization to add assignment: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewAllAssignmentsAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator || currentUserRole == UserRoles.Teacher;
            _logger.LogInformation($"User {currentUserId} authorization to view all assignments: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewAssignmentDetailsAsync(int currentUserId, int assignmentId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {currentUserRole ?? "N/A"}) attempting to view assignment details for Assignment ID {assignmentId}.");

            if (currentUserRole == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can view any assignment details.");
                return true;
            }

            var assignment = await _context.TeacherSubjectGroupAssignments
                                           .FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == assignmentId);
            if (assignment == null)
            {
                _logger.LogWarning($"Authorization failed: Assignment with ID {assignmentId} not found for viewing.");
                throw new NotFoundException($"Assignment with ID {assignmentId} not found.");
            }

            if (currentUserRole == UserRoles.Teacher)
            {
                var teacherProfile = await GetTeacherByIdAsync(currentUserId); // Get Teacher profile by User ID
                bool isTeacherAssigned = teacherProfile != null && assignment.TeacherId == teacherProfile.TeacherId;
                if (isTeacherAssigned)
                {
                    _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) is assigned to Assignment ID {assignmentId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Teacher (User ID: {currentUserId}) is not assigned to Assignment ID {assignmentId}.");
                }
                return isTeacherAssigned;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {currentUserRole ?? "N/A"}) cannot view assignment details for Assignment ID {assignmentId}.");
            return false;
        }

        public async Task<bool> CanUserUpdateAssignmentAsync(int currentUserId, int assignmentId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator; // Only admins for now
            // If teachers can update their own assignments:
            // if (currentUserRole == UserRoles.Teacher) {
            //     var assignment = await _context.TeacherSubjectGroupAssignments.AsNoTracking().FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == assignmentId);
            //     var teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == currentUserId);
            //     if (assignment != null && teacher != null && assignment.TeacherId == teacher.TeacherId) return true;
            // }
            _logger.LogInformation($"User {currentUserId} authorization to update assignment {assignmentId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserDeleteAssignmentAsync(int currentUserId, int assignmentId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            bool authorized = currentUserRole == UserRoles.Administrator; // Only admins for now
            // If teachers can delete their own assignments:
            // if (currentUserRole == UserRoles.Teacher) {
            //     var assignment = await _context.TeacherSubjectGroupAssignments.AsNoTracking().FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == assignmentId);
            //     var teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == currentUserId);
            //     if (assignment != null && teacher != null && assignment.TeacherId == teacher.TeacherId) return true;
            // }
            _logger.LogInformation($"User {currentUserId} authorization to delete assignment {assignmentId}: {authorized} (Role: {currentUserRole ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> IsUserAssignedToTeacherSubjectGroupAssignment(int userId, int teacherSubjectGroupAssignmentId)
        {
            // This method name is confusing and seems to be for a specific permission check.
            // Let's assume it checks if the given userId (who is a teacher) is assigned to that specific TSGA.
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .Include(u => u.Teacher)
                                     .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning($"Authorization failed: User with ID {userId} not found.");
                return false;
            }

            if (user.Role?.Name == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted for Admin user {userId}.");
                return true;
            }

            if (user.Role?.Name == UserRoles.Teacher && user.Teacher != null)
            {
                var isAssigned = await _context.TeacherSubjectGroupAssignments
                                                .AnyAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == teacherSubjectGroupAssignmentId &&
                                                                  tsga.TeacherId == user.Teacher.TeacherId);
                if (!isAssigned)
                {
                    _logger.LogWarning($"Authorization failed: Teacher (User ID: {userId}) is not assigned to TeacherSubjectGroupAssignment ID {teacherSubjectGroupAssignmentId}.");
                }
                else
                {
                    _logger.LogInformation($"Authorization granted: Teacher (User ID: {userId}) is assigned to TeacherSubjectGroupAssignment ID {teacherSubjectGroupAssignmentId}.");
                }
                return isAssigned;
            }

            _logger.LogWarning($"Authorization failed: User with ID {userId} is not an Admin or a Teacher, or has no Teacher profile.");
            return false;
        }

        public async Task<bool> CanUserAddGradeAsync(int currentUserId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            bool authorized = role == UserRoles.Administrator || role == UserRoles.Teacher;
            _logger.LogInformation($"User {currentUserId} authorization to add grade: {authorized} (Role: {role ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewAllGradesAsync(int currentUserId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            bool authorized = role == UserRoles.Administrator || role == UserRoles.Teacher;
            _logger.LogInformation($"User {currentUserId} authorization to view all grades: {authorized} (Role: {role ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewGradeDetailsAsync(int currentUserId, int gradeId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {role ?? "N/A"}) attempting to view grade details for Grade ID {gradeId}.");

            if (role == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can view any grade details.");
                return true;
            }

            var grade = await _context.Grades
                                     .Include(g => g.Student)
                                     .Include(g => g.TeacherSubjectGroupAssignment)
                                         .ThenInclude(tsga => tsga.Teacher)
                                     .Include(g => g.TeacherSubjectGroupAssignment.Subject)
                                     .Include(g => g.TeacherSubjectGroupAssignment.Semester)
                                     .FirstOrDefaultAsync(g => g.GradeId == gradeId);
            if (grade == null)
            {
                _logger.LogWarning($"Authorization failed: Grade with ID {gradeId} not found for viewing.");
                throw new NotFoundException($"Grade with ID {gradeId} not found.");
            }

            if (role == UserRoles.Teacher)
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == currentUserId);
                bool isTeacherAssignedToGrade = teacher != null && grade.TeacherSubjectGroupAssignment?.TeacherId == teacher.TeacherId;
                if (isTeacherAssignedToGrade)
                {
                    _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) is assigned to grade {gradeId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Teacher (User ID: {currentUserId}) is not assigned to grade {gradeId}.");
                }
                return isTeacherAssignedToGrade;
            }

            if (role == UserRoles.Student)
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                bool isOwnGrade = student != null && grade.StudentId == student.StudentId;
                if (isOwnGrade)
                {
                    _logger.LogInformation($"Authorization granted: Student (User ID: {currentUserId}) can view their own Grade ID {gradeId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Student (User ID: {currentUserId}) cannot view Grade ID {gradeId} (not their own).");
                }
                return isOwnGrade;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {role ?? "N/A"}) cannot view grade details for Grade ID {gradeId}.");
            return false;
        }

        public async Task<bool> CanUserUpdateGradeAsync(int currentUserId, int gradeId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {role ?? "N/A"}) attempting to update grade {gradeId}.");

            if (role == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can update any grade.");
                return true;
            }

            var grade = await _context.Grades
                                 .Include(g => g.TeacherSubjectGroupAssignment)
                                     .ThenInclude(tsga => tsga.Teacher)
                                 .FirstOrDefaultAsync(g => g.GradeId == gradeId);
            if (grade == null)
            {
                _logger.LogWarning($"Authorization failed: Grade with ID {gradeId} not found for update.");
                throw new NotFoundException($"Grade with ID {gradeId} not found.");
            }

            if (role == UserRoles.Teacher)
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == currentUserId);
                bool isTeacherAssignedToGrade = teacher != null && grade.TeacherSubjectGroupAssignment?.TeacherId == teacher.TeacherId;
                if (isTeacherAssignedToGrade)
                {
                    _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) is the owner of Grade ID {gradeId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Teacher (User ID: {currentUserId}) is not the owner of Grade ID {gradeId}.");
                }
                return isTeacherAssignedToGrade;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {role ?? "N/A"}) cannot update grade {gradeId}.");
            return false;
        }

        public async Task<bool> CanUserDeleteGradeAsync(int currentUserId, int gradeId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {role ?? "N/A"}) attempting to delete grade {gradeId}.");

            if (role == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can delete any grade.");
                return true;
            }

            var grade = await _context.Grades
                                 .Include(g => g.TeacherSubjectGroupAssignment)
                                     .ThenInclude(tsga => tsga.Teacher)
                                 .FirstOrDefaultAsync(g => g.GradeId == gradeId);
            if (grade == null)
            {
                _logger.LogWarning($"Authorization failed: Grade with ID {gradeId} not found for deletion.");
                throw new NotFoundException($"Grade with ID {gradeId} not found.");
            }

            if (role == UserRoles.Teacher)
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == currentUserId);
                bool isTeacherAssignedToGrade = teacher != null && grade.TeacherSubjectGroupAssignment?.TeacherId == teacher.TeacherId;
                if (isTeacherAssignedToGrade)
                {
                    _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) is the owner of Grade ID {gradeId}.");
                }
                else
                {
                    _logger.LogWarning($"Authorization failed: Teacher (User ID: {currentUserId}) is not the owner of Grade ID {gradeId}.");
                }
                return isTeacherAssignedToGrade;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {role ?? "N/A"}) cannot delete grade {gradeId}.");
            return false;
        }

        public async Task<bool> CanTeacherAssignGrade(int currentUserId, int studentId, int subjectId, int semesterId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {currentUserRole ?? "N/A"}) attempting to assign grade to student {studentId} for subject {subjectId} in semester {semesterId}.");

            if (currentUserRole == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can assign any grade.");
                return true;
            }
            if (currentUserRole != UserRoles.Teacher)
            {
                _logger.LogWarning($"Authorization failed: User {currentUserId} is not a Teacher.");
                return false;
            }

            var teacherProfile = await GetTeacherByIdAsync(currentUserId); // Предполагается, что возвращает TeacherDto
            if (teacherProfile == null || teacherProfile.TeacherId == null) // Проверка на null и TeacherId
            {
                _logger.LogWarning($"Authorization failed: Teacher profile or TeacherId not found for user {currentUserId}.");
                throw new NotFoundException($"Teacher profile not found for user ID {currentUserId}.");
            }

            var studentProfile = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            // ИСПРАВЛЕНИЕ: Удаляем .HasValue, так как studentProfile.GroupId является int.
            // Вместо этого проверяем, что GroupId действителен (например, > 0).
            if (studentProfile == null || studentProfile.GroupId <= 0) // Если 0 означает "нет группы"
            {
                _logger.LogWarning($"Authorization failed: Student profile (ID: {studentId}) not found or has no valid group assigned (GroupId: {studentProfile?.GroupId ?? 0}).");
                throw new NotFoundException($"Student with ID {studentId} not found or has no assigned group.");
            }

            // Check if the teacher is assigned to the student's group for this subject and semester
            bool isAssigned = await _context.TeacherSubjectGroupAssignments
                .AnyAsync(tsga => tsga.TeacherId == teacherProfile.TeacherId && // Используем TeacherId из DTO
                                 tsga.SubjectId == subjectId &&
                                 tsga.GroupId == studentProfile.GroupId && // ИСПРАВЛЕНИЕ: используем studentProfile.GroupId напрямую
                                 tsga.SemesterId == semesterId);

            if (isAssigned)
            {
                _logger.LogInformation($"Authorization granted: Teacher (User ID: {currentUserId}) is assigned to teach student's group {studentProfile.GroupId} for this subject/semester."); // ИСПРАВЛЕНИЕ
            }
            else
            {
                _logger.LogWarning($"Authorization failed: Teacher (User ID: {currentUserId}) is not assigned to teach student's group {studentProfile.GroupId} for subject {subjectId} during semester {semesterId}."); // ИСПРАВЛЕНИЕ
            }
            return isAssigned;
        }

        public async Task<bool> CanTeacherViewStudentGrades(int teacherProfileId, int studentId)
        {
            _logger.LogInformation($"Teacher {teacherProfileId} attempting to view grades for student {studentId}.");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                _logger.LogWarning($"Authorization failed: Student with ID {studentId} not found.");
                throw new NotFoundException($"Student with ID {studentId} not found.");
            }

            // ИСПРАВЛЕНИЕ: Удаляем .HasValue, так как student.GroupId является int.
            // Вместо этого проверяем, что GroupId действителен (например, > 0).
            if (student.GroupId <= 0) // Если 0 означает "нет группы" или невалидный ID
            {
                _logger.LogWarning($"Authorization failed: Student with ID {studentId} has no valid group assigned (GroupId: {student.GroupId}).");
                // Можно выбросить BadRequestException или просто вернуть false, в зависимости от того, как вы хотите обрабатывать этот случай.
                // Если "нет группы" - это допустимое состояние для студента, но не для просмотра оценок преподавателем, то false.
                // Если же это всегда ошибка данных, то throw new BadRequestException.
                throw new BadRequestException($"Student with ID {studentId} has no valid group assigned.");
            }

            // Check if the teacher has any assignment in the student's group
            // ИСПРАВЛЕНИЕ: Используем student.GroupId напрямую, без .Value
            bool hasAssignmentInStudentGroup = await _context.TeacherSubjectGroupAssignments
                .AnyAsync(tsga => tsga.TeacherId == teacherProfileId &&
                                 tsga.GroupId == student.GroupId); // ИСПРАВЛЕНИЕ: используем student.GroupId напрямую

            if (hasAssignmentInStudentGroup)
            {
                _logger.LogInformation($"Authorization granted: Teacher {teacherProfileId} has assignments in student {studentId}'s group {student.GroupId}."); // ИСПРАВЛЕНИЕ
            }
            else
            {
                _logger.LogWarning($"Authorization failed: Teacher {teacherProfileId} has no assignments in student {studentId}'s group {student.GroupId}."); // ИСПРАВЛЕНИЕ
            }
            return hasAssignmentInStudentGroup;
        }


        // --- Role-Related Authorization Methods ---
        public async Task<bool> CanUserViewAllRolesAsync(int currentUserId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            bool authorized = role == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to view all roles: {authorized} (Role: {role ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserViewRoleDetailsAsync(int currentUserId, int roleId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {role ?? "N/A"}) attempting to view role details for Role ID {roleId}.");

            if (string.IsNullOrEmpty(role))
            {
                _logger.LogWarning($"Authorization failed: User {currentUserId} has no role assigned.");
                return false;
            }

            if (role == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can view any role details.");
                return true;
            }

            var currentUserProfile = await _context.Users
                                                   .Include(u => u.Role)
                                                   .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUserProfile != null && currentUserProfile.RoleId == roleId)
            {
                _logger.LogInformation($"Authorization granted: User {currentUserId} can view their own Role ID {roleId}.");
                return true;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {role ?? "N/A"}) cannot view Role ID {roleId}.");
            return false;
        }

        public async Task<bool> CanUserCreateRoleAsync(int currentUserId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            bool authorized = role == UserRoles.Administrator;
            _logger.LogInformation($"User {currentUserId} authorization to create role: {authorized} (Role: {role ?? "N/A"}).");
            return authorized;
        }

        public async Task<bool> CanUserUpdateRoleAsync(int currentUserId, int roleId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {role ?? "N/A"}) attempting to update role {roleId}.");

            if (string.IsNullOrEmpty(role))
            {
                _logger.LogWarning($"Authorization failed: User {currentUserId} has no role assigned.");
                return false;
            }

            if (role == UserRoles.Administrator)
            {
                _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can update any role.");
                return true;
            }

            _logger.LogWarning($"Authorization failed: User {currentUserId} (Role: {role ?? "N/A"}) cannot update role {roleId}.");
            return false;
        }

        public async Task<bool> CanUserDeleteRoleAsync(int currentUserId, int roleId)
        {
            var role = await GetUserRoleAsync(currentUserId);
            _logger.LogInformation($"User {currentUserId} (Role: {role ?? "N/A"}) attempting to delete role {roleId}.");

            if (string.IsNullOrEmpty(role) || role != UserRoles.Administrator)
            {
                _logger.LogWarning($"Authorization failed: User {currentUserId} is not an Administrator or has no role.");
                return false;
            }

            var roleToDelete = await _context.Roles.FindAsync(roleId);
            if (roleToDelete == null)
            {
                _logger.LogWarning($"Authorization failed: Role with ID {roleId} not found for deletion.");
                return false;
            }

            var criticalRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                UserRoles.Administrator,
                UserRoles.Teacher,
                UserRoles.Student
            };

            if (criticalRoles.Contains(roleToDelete.Name))
            {
                _logger.LogWarning($"Authorization failed: Attempted to delete critical system role '{roleToDelete.Name}' (ID: {roleId}).");
                return false;
            }

            _logger.LogInformation($"Authorization granted: Admin user {currentUserId} can delete role {roleId} ('{roleToDelete.Name}').");
            return true;
        }


        // Helper for JWT token generation
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretString = jwtSettings["Secret"];
            var issuerString = jwtSettings["Issuer"];
            var audienceString = jwtSettings["Audience"];
            var expirationMinutesString = jwtSettings["ExpirationMinutes"];

            if (string.IsNullOrEmpty(secretString)) throw new InvalidOperationException("JWT Secret is not configured in appsettings.");
            if (!double.TryParse(expirationMinutesString, out double expirationMinutes)) expirationMinutes = 15;

            var key = Encoding.UTF8.GetBytes(secretString);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "User")
            };

            // Add GroupId to claims ONLY if the user is a STUDENT and has a valid GroupId.
            // ИСПРАВЛЕНИЕ: Удаляем .HasValue и .Value, так как user.Student.GroupId является int.
            // Проверяем, что Student и GroupId существуют и GroupId > 0 (валидный ID).
            if (user.Role?.Name == UserRoles.Student && user.Student != null && user.Student.GroupId > 0)
            {
                claims.Add(new Claim("groupId", user.Student.GroupId.ToString())); // ИСПРАВЛЕНИЕ: используем напрямую
                _logger.LogInformation($"Added groupId {user.Student.GroupId} for student user {user.Username} to JWT claims."); // ИСПРАВЛЕНИЕ: используем напрямую
            }
            // For Teacher, typically their direct assignments are what matters, not a single groupId.
            // For Admin with groupId: null, no groupId claim is added, which is correct for a global admin.


            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = issuerString,
                Audience = audienceString,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
