// Path: StudentPerformance.Api/Services/UserService.cs


using StudentPerformance.Api.Data;
using StudentPerformance.Api.Data.Entities;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

using StudentPerformance.Api.Models.QueryParameters;
// using Microsoft.AspNetCore.Identity; // Removed as it's not used with custom IPasswordHasher
using StudentPerformance.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace StudentPerformance.Api.Services
{
    // It's highly recommended to define roles as constants for consistency
    public static class UserRoles
    {
        public const string Administrator = "Администратор";
        public const string Teacher = "Преподаватель";
        public const string Student = "Студент";
    }

    /// <summary>
    /// Provides services for user authentication, role management, and granular authorization checks.
    /// In a larger application, consider breaking this into smaller, more focused services
    /// (e.g., AuthService, GroupService, AssignmentService, GradeService, SemesterService, StudentService, SubjectService, TeacherService).
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher; // <--- CORRECTED: IPasswordHasher<User>
        private readonly IConfiguration _configuration;

        // Constructor
        public UserService(ApplicationDbContext context, IMapper mapper,
                             IPasswordHasher<User> passwordHasher, IConfiguration configuration)// <--- CORRECTED: IPasswordHasher<User>
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }


        // --- Authentication & Registration Methods ---



        /// <summary>

        /// Authenticates a user based on username and password.

        /// </summary>

        /// <param name="username">The user's username.</param>

        /// <param name="password">The user's password (should be hashed in a real application).</param>

        /// <returns>The User entity if authentication is successful, otherwise null.</returns>
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Role) // Include role for DTO mapping
                .ToListAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        /// <summary>
        /// Retrieves a list of users by their role name.
        /// </summary>
        /// <param name="roleName">The name of the role.</param>
        /// <returns>A list of UserDto objects.</returns>
        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName)
        {
            var users = await _context.Users
                .Include(u => u.Role) // Include the Role to filter by role name
                .Where(u => u.Role != null && u.Role.Name == roleName)
                .ToListAsync(); // Get a List of User entities

            // Map the list of User entities to a list of UserDto
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }


        // Rename AuthenticateAsync to AuthenticateUserAsync and adjust parameters/return
        public async Task<AuthenticationResult?> AuthenticateUserAsync(string username, string password)
        {
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return new AuthenticationResult { Success = false, Errors = new[] { "Invalid credentials" } };
            }

            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return new AuthenticationResult { Success = false, Errors = new[] { "Invalid credentials" } };
            }

            // Authentication successful: Generate JWT Token
            var token = GenerateJwtToken(user);

            // Map user to UserDto
            var userDto = _mapper.Map<UserDto>(user);

            return new AuthenticationResult
            {
                User = userDto,
                Token = token,
                Success = true
            };
        }

        /// <summary>
        /// Generates a JWT token for the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user entity.</param>
        /// <returns>The generated JWT token string.</returns>
        private string GenerateJwtToken(User user)
        {
            // THIS IS WHERE YOU'RE GETTING THE ERROR
            var jwtSettings = _configuration.GetSection("JwtSettings"); // <--- _configuration IS USED HERE
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "User")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpirationMinutes"])),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }



        /// <summary>

        /// Registers a new user and creates their associated profile (Student or Teacher).

        /// </summary>

        /// <param name="request">The registration data.</param>

        /// <returns>The newly created User entity, or null if the username already exists.</returns>
        /// 
        public async Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            // Implementation logic here.
            // This is likely similar to your UpdateStudentAsync or UpdateTeacherAsync,
            // but for a generic User entity.
            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate == null)
            {
                return false;
            }

            // Map properties from request to userToUpdate
            // Example:
            // userToUpdate.Email = request.Email;
            // userToUpdate.FirstName = request.FirstName;
            // userToUpdate.LastName = request.LastName;
            // userToUpdate.Username = request.Username; // Be careful with username changes, might need separate logic
            // userToUpdate.RoleId = request.RoleId; // Ensure role exists before assigning

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == userId))
                {
                    return false;
                }
                throw;
            }
            catch (Exception)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<User?> RegisterUserAsync(RegisterRequest request) // <--- Adjusted return type to Task<User?>
        {
            // 1. Проверка, существует ли пользователь с таким именем
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return null; // Имя пользователя уже занято
            }

            // 2. Найти RoleId на основе UserType из запроса
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.UserType);
            if (role == null)
            {
                throw new ArgumentException($"UserType '{request.UserType}' is not a valid role.");
            }

            // 3. Создать нового пользователя
            var newUser = new User
            {
                Username = request.Username,
                // Temporarily omit PasswordHash here, or set to string.Empty
                RoleId = role.RoleId, // Ensure role.RoleId is correctly assigned
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            // CRITICAL: HASH THE PASSWORD NOW THAT newUser IS DECLARED
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password); // <--- CORRECTED!

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(); // Сохраняем, чтобы получить newUser.Id
            // 4. Создать связанный профиль на основе RoleName
            if (role.Name == "Студент")
            {
                if (!request.GroupId.HasValue)
                {
                    throw new ArgumentException("GroupId is required for student registration.");
                }
                var studentProfile = new Student { UserId = newUser.Id, GroupId = request.GroupId.Value };
                _context.Students.Add(studentProfile);
            }
            else if (role.Name == "Преподаватель")
            {
                var teacherProfile = new Teacher { UserId = newUser.Id };
                _context.Teachers.Add(teacherProfile);
            }
            // Handle other roles or no-profile roles

            await _context.SaveChangesAsync(); // Сохраняем профиль (if student/teacher profile was added)

            // IMPORTANT: Return the User entity directly, as IUserService now expects Task<User?>
            return newUser;
        }


        // --- User Profile & Role Retrieval Methods ---



        /// <summary>

        /// Retrieves a user's basic information by their ID.

        /// </summary>

        /// <param name="userId">The ID of the user.</param>

        /// <returns>A UserDto, or null if not found.</returns>
        /// 
        public async Task<bool> DeleteUserAsync(int userId)
        {
            var userToDelete = await _context.Users
                                                .Include(u => u.Student) // If user can be a student
                                                .Include(u => u.Teacher) // If user can be a teacher
                                                .FirstOrDefaultAsync(u => u.Id == userId); // Assuming Id is the PK

            if (userToDelete == null)
            {
                return false;
            }

            // IMPORTANT: Handle cascading deletes or dependencies here.
            // If a User is associated with a Student or Teacher, deleting the User
            // might leave orphaned Student/Teacher records or cause foreign key errors.
            // Consider soft delete or specific business rules.
            // For a hard delete, you might need to explicitly remove related entities first
            // if EF Core is not configured for cascade delete for these relationships.

            if (userToDelete.Student != null)
            {
                _context.Students.Remove(userToDelete.Student);
            }
            if (userToDelete.Teacher != null)
            {
                _context.Teachers.Remove(userToDelete.Teacher);
            }

            _context.Users.Remove(userToDelete);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)

        {

            var user = await _context.Users.FindAsync(userId);

            return _mapper.Map<UserDto>(user);

        }



        /// <summary>

        /// Retrieves user information including their role (same as GetUserByIdAsync for now).

        /// </summary>

        /// <param name="userId">The ID of the user.</param>

        /// <returns>A UserDto, or null if not found.</returns>

        public async Task<UserDto?> GetUserWithRoleByIdAsync(int userId)

        {

            return await GetUserByIdAsync(userId); // Role is part of UserDto

        }



        /// <summary>

        /// Retrieves a list of all users.

        /// </summary>

        /// <returns>A list of UserDto objects.</returns>




        /// <summary>

        /// Retrieves users filtered by their role.

        /// </summary>

        /// <param name="roleName">The name of the role (e.g., "Администратор", "Преподаватель", "Студент").</param>

        /// <returns>A list of UserDto objects.</returns>



        /// <summary>

        /// Gets the role (UserType) of a specific user.

        /// </summary>

        /// <param name="userId">The ID of the user.</param>

        /// <returns>The user's role string, or null if the user is not found.</returns>

        public async Task<string?> GetUserRoleAsync(int userId)
        {
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Id == userId);
            return user?.Role?.Name;
        }






        /// <summary>

        /// Retrieves the Teacher profile associated with a user ID.

        /// </summary>

        /// <param name="userId">The ID of the user.</param>

        /// <returns>The Teacher entity, or null if not found.</returns>

        public async Task<TeacherDto?> GetTeacherByIdAsync(int userId) // Return type here
        {
            var teacher = await _context.Teachers
                                        .Include(t => t.User) // Include User for DTO mapping
                                        .FirstOrDefaultAsync(t => t.UserId == userId);
            return _mapper.Map<TeacherDto>(teacher);
        }

        public async Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAssignmentsByTeacherIdAsync(int teacherId) // Changed List to IEnumerable
        {
            var assignments = await _context.TeacherSubjectGroupAssignments
                .Where(tsga => tsga.TeacherId == teacherId)
                .Include(tsga => tsga.Teacher)
                    .ThenInclude(t => t.User)
                .Include(tsga => tsga.Subject)
                .Include(tsga => tsga.Group)
                .Include(tsga => tsga.Semester)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TeacherSubjectGroupAssignmentDto>>(assignments);
        }



        /// <summary>

        /// Retrieves the Student profile associated with a user ID.

        /// </summary>

        /// <param name="userId">The ID of the user.</param>

        /// <returns>The Student entity, or null if not found.</returns>

        public async Task<StudentDto?> GetStudentByIdAsync(int userId) // The return type in implementation
        {
            var student = await _context.Students
                                        .Include(s => s.User)
                                        .Include(s => s.Group)
                                        .FirstOrDefaultAsync(s => s.UserId == userId);
            return _mapper.Map<StudentDto>(student);
        }


        // --- Group Management Methods ---



        /// <summary>

        /// Adds a new student group.

        /// </summary>

        /// <param name="request">Data for the new group.</param>

        /// <returns>The DTO of the newly created group, or null if a group with the same name already exists.</returns>

        public async Task<GroupDto?> AddGroupAsync(AddGroupRequest request)

        {

            var existingGroup = await _context.Groups

              .AnyAsync(g => g.Name == request.Name);



            if (existingGroup) return null; // Group name already exists



            var group = _mapper.Map<Group>(request);

            _context.Groups.Add(group);

            await _context.SaveChangesAsync();

            return _mapper.Map<GroupDto>(group);

        }



        /// <summary>

        /// Retrieves a list of all student groups.

        /// </summary>

        /// <returns>A list of GroupDto objects.</returns>

        public async Task<List<GroupDto>> GetAllGroupsAsync()

        {

            var groups = await _context.Groups.ToListAsync();

            return _mapper.Map<List<GroupDto>>(groups);

        }



        /// <summary>

        /// Retrieves a specific student group by its ID.

        /// </summary>

        /// <param name="groupId">The ID of the group.</param>

        /// <returns>The DTO of the group if found, otherwise null.</returns>

        public async Task<GroupDto?> GetGroupByIdAsync(int groupId)

        {

            var group = await _context.Groups.FindAsync(groupId);

            return _mapper.Map<GroupDto>(group);

        }



        /// <summary>

        /// Updates an existing student group.

        /// </summary>

        /// <param name="groupId">The ID of the group to update.</param>

        /// <param name="request">The updated data for the group.</param>

        /// <returns>True if the group was updated, false if not found or a name conflict occurs.</returns>

        public async Task<bool> UpdateGroupAsync(int groupId, UpdateGroupRequest request) // <--- Make sure it's UpdateGroupRequest here

        {

            var groupToUpdate = await _context.Groups.FindAsync(groupId);

            if (groupToUpdate == null) return false;



            // Check for name conflict with other groups

            var conflictExists = await _context.Groups

        .AnyAsync(g => g.Name == request.Name && g.GroupId != groupId);



            if (conflictExists) return false; // Name already taken by another group



            groupToUpdate.Name = request.Name;

            try

            {

                await _context.SaveChangesAsync();

                return true;

            }

            catch (DbUpdateConcurrencyException)

            {

                if (!_context.Groups.Any(e => e.GroupId == groupId)) return false; // Group was deleted

                throw; // Re-throw for genuine concurrency issues

            }

            catch (Exception)

            {

                return false; // Other errors

            }

        }



        /// <summary>

        /// Deletes a student group by its ID.

        /// </summary>

        /// <param name="groupId">The ID of the group to delete.</param>

        /// <returns>True if the group was deleted, false if not found.</returns>

        public async Task<bool> DeleteGroupAsync(int groupId)

        {

            var groupToDelete = await _context.Groups.FindAsync(groupId);

            if (groupToDelete == null) return false;



            // Consider checking for dependent students or assignments before deleting

            // to prevent foreign key constraint errors or unwanted data loss.



            _context.Groups.Remove(groupToDelete);

            await _context.SaveChangesAsync();

            return true;

        }



        // --- Semester Management Methods ---



        /// <summary>

        /// Adds a new academic semester.

        /// </summary>

        /// <param name="request">Data for the new semester.</param>

        /// <returns>The DTO of the newly created semester, or null if invalid dates or name conflict.</returns>

        public async Task<SemesterDto?> AddSemesterAsync(AddSemesterRequest request)

        {

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate <= request.StartDate)

            {

                return null; // End date cannot be before or same as start date

            }



            var existingSemester = await _context.Semesters

              .AnyAsync(s => s.Name == request.Name);



            if (existingSemester) return null; // Semester name already exists



            var semester = _mapper.Map<Semester>(request);

            _context.Semesters.Add(semester);

            await _context.SaveChangesAsync();

            return _mapper.Map<SemesterDto>(semester);

        }



        /// <summary>

        /// Retrieves a list of all academic semesters.

        /// </summary>

        /// <returns>A list of SemesterDto objects.</returns>

        public async Task<IEnumerable<SemesterDto>> GetAllSemestersAsync()
        {
            var semesters = await _context.Semesters.ToListAsync();
            return _mapper.Map<IEnumerable<SemesterDto>>(semesters);
        }


        /// <summary>

        /// Retrieves a specific academic semester by its ID.

        /// </summary>

        /// <param name="semesterId">The ID of the semester.</param>

        /// <returns>The DTO of the semester if found, otherwise null.</returns>

        public async Task<SemesterDto?> GetSemesterByIdAsync(int semesterId)

        {

            var semester = await _context.Semesters.FindAsync(semesterId);

            return _mapper.Map<SemesterDto>(semester);

        }



        /// <summary>

        /// Updates an existing academic semester.

        /// </summary>

        /// <param name="semesterId">The ID of the semester to update.</param>

        /// <param name="request">The updated data for the semester.</param>

        /// <returns>True if the semester was updated, false if not found, invalid dates, or name conflict.</returns>

        public async Task<bool> UpdateSemesterAsync(int semesterId, UpdateSemesterRequest request)

        {

            var semesterToUpdate = await _context.Semesters.FindAsync(semesterId);

            if (semesterToUpdate == null) return false; // Семестр не найден



            // 1. Валидация дат: Проверяем только если ОБЕ даты предоставлены

            if (request.StartDate.HasValue && request.EndDate.HasValue)

            {

                if (request.EndDate.Value <= request.StartDate.Value) // Используем .Value, т.к. мы уже проверили HasValue

                {

                    return false; // Некорректные даты: Дата окончания не может быть раньше или равна дате начала

                }

            }

            // Если одна из дат (или обе) не предоставлена, эта проверка пропускается.

            // Это подразумевает, что можно обновить только имя, оставив даты прежними,

            // или обновить только одну дату, если это допустимо.



            // 2. Проверка на конфликт имени

            var conflictExists = await _context.Semesters

        .AnyAsync(s => s.Name == request.Name && s.SemesterId != semesterId);



            if (conflictExists) return false; // Конфликт имени: семестр с таким именем уже существует



            // 3. Обновление полей сущности

            semesterToUpdate.Name = request.Name; // Имя, вероятно, всегда обязательное и неnullable в запросе



            // Обновляем StartDate только если она предоставлена в запросе

            if (request.StartDate.HasValue)

            {

                semesterToUpdate.StartDate = request.StartDate.Value;

            }



            // Обновляем EndDate только если она предоставлена в запросе

            if (request.EndDate.HasValue)

            {

                semesterToUpdate.EndDate = request.EndDate.Value;

            }



            // 4. Сохранение изменений

            try

            {

                await _context.SaveChangesAsync();

                return true; // Успешно обновлено

            }

            catch (DbUpdateConcurrencyException)

            {

                // Обработка конфликтов параллельного доступа (если запись была изменена другим пользователем)

                if (!_context.Semesters.Any(e => e.SemesterId == semesterId))

                {

                    return false; // Запись уже удалена другим пользователем

                }

                throw; // Перевыбросить исключение, если конфликт реальный и запись все еще существует

            }

            catch (Exception)

            {

                // Общая обработка ошибок (можно добавить логирование для отладки)

                return false;

            }

        }



        /// <summary>

        /// Deletes an academic semester by its ID.

        /// </summary>

        /// <param name="semesterId">The ID of the semester to delete.</param>

        /// <returns>True if the semester was deleted, false if not found.</returns>

        public async Task<bool> DeleteSemesterAsync(int semesterId)

        {

            var semesterToDelete = await _context.Semesters.FindAsync(semesterId);

            if (semesterToDelete == null) return false;



            // Consider checking for dependent assignments or grades before deleting.



            _context.Semesters.Remove(semesterToDelete);

            await _context.SaveChangesAsync();

            return true;

        }



        // --- Student Management Methods ---



        /// <summary>

        /// Adds a new student, creating a new user account and linking it to a student profile.

        /// </summary>

        /// <param name="request">Data for the new student and their user account.</param>

        /// <returns>The DTO of the newly created student, or null if username/login already exists or group not found.</returns>

        public async Task<StudentDto?> AddStudentAsync(AddStudentRequest request)

        {

            // 1. Check if the login (username) already exists

            if (await _context.Users.AnyAsync(u => u.Username == request.Login))

            {

                // Возможно, стоит выбросить исключение с сообщением об ошибке, а не возвращать null

                // throw new ArgumentException("Login already taken.");

                return null; // Login already taken

            }



            // 2. Check if the specified GroupId exists

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == request.GroupId);

            if (group == null) // Проверяем существование группы

            {

                // throw new ArgumentException($"Group with ID {request.GroupId} not found.");

                return null; // Group not found

            }



            // 3. Найти RoleId для "Студент"

            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Студент");

            if (studentRole == null)

            {

                // Это критическая ошибка: роль "Студент" должна существовать в базе данных

                throw new InvalidOperationException("Role 'Студент' not found in the database. Please seed roles.");

            }



            // 4. Create the User entity

            var newUser = _mapper.Map<User>(request);

            newUser.RoleId = studentRole.RoleId; // !!! ИСПРАВЛЕНО: Присваиваем RoleId для "Студент"

            // newUser.UserType = "Студент"; // УДАЛЕНО: Это свойство больше не существует

            // В реальном приложении: хешируйте newUser.PasswordHash здесь

            // newUser.PasswordHash = _passwordHasher.HashPassword(request.Password); // Пример



            _context.Users.Add(newUser);

            await _context.SaveChangesAsync(); // Save to get the new User.Id



            // 5. Create the Student entity, linking it to the new User

            var newStudent = _mapper.Map<Student>(request); // Маппит общие поля из запроса

            newStudent.UserId = newUser.Id; // Link to the newly created user

            newStudent.GroupId = request.GroupId; // Ensure GroupId is set (уже проверено на существование)



            _context.Students.Add(newStudent);

            await _context.SaveChangesAsync();



            // 6. Load navigation properties for the DTO mapping

            // Если вы используете AutoMapper, часто можно настроить его для автоматической загрузки

            // связанных данных, если они были загружены в исходную сущность.

            // Однако, явная загрузка здесь тоже работает, если вам нужна 100% уверенность,

            // что данные будут доступны для маппинга.

            await _context.Entry(newStudent)

        .Reference(s => s.User).LoadAsync();

            await _context.Entry(newStudent)

              .Reference(s => s.Group).LoadAsync();



            return _mapper.Map<StudentDto>(newStudent);

        }



        /// <summary>

        /// Retrieves a list of all students.

        /// </summary>

        /// <returns>A list of StudentDto objects.</returns>
        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync() // <--- No parameters here
        {
            var students = await _context.Students
                                        .Include(s => s.User) // Include related User data
                                        .Include(s => s.Group) // Include related Group data
                                        .ToListAsync(); // Fetch all students

            return _mapper.Map<IEnumerable<StudentDto>>(students); // Map them to DTOs
        }

        /// <summary>

        /// Retrieves a specific student by their ID.

        /// </summary>

        /// <param name="studentId">The ID of the student.</param>

        /// <returns>The DTO of the student if found, otherwise null.</returns>

        public async Task<StudentDto?> GetStudentDetailsByIdAsync(int studentId)

        {

            var student = await _context.Students

              .Include(s => s.User)

              .Include(s => s.Group)

              .FirstOrDefaultAsync(s => s.StudentId == studentId);

            return _mapper.Map<StudentDto>(student);

        }



        /// <summary>

        /// Updates an existing student's details and their associated user account details.

        /// </summary>

        /// <param name="studentId">The ID of the student to update.</param>

        /// <param name="request">The updated data for the student and their user account.</param>

        /// <returns>True if the student was updated, false if not found or no changes.</returns>

        public async Task<bool> UpdateStudentAsync(int studentId, UpdateStudentRequest request)

        {

            var studentToUpdate = await _context.Students

              .Include(s => s.User) // Include User to update it as well

                      .FirstOrDefaultAsync(s => s.StudentId == studentId);



            if (studentToUpdate == null || studentToUpdate.User == null)

            {

                return false; // Student or associated user not found

            }

            // --- Update User properties ---

            // Assuming FullName in DTO needs to be parsed for FirstName/LastName

            var names = request.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            studentToUpdate.User.FirstName = names.ElementAtOrDefault(0) ?? string.Empty;

            studentToUpdate.User.LastName = names.ElementAtOrDefault(1) ?? string.Empty;



            // Email может быть nullable в запросе и сущности User, поэтому проверяем:

            if (request.Email != null) // Если Email предоставлен (не null)

            {

                studentToUpdate.User.Email = request.Email;

            }

            // Если request.Email == null, это означает, что мы не хотим его менять.



            // --- Update Student-specific properties ---

            // ИСПРАВЛЕНИЕ ЗДЕСЬ для DateOfBirth

            if (request.DateOfBirth.HasValue) // Проверяем, есть ли значение

            {

                studentToUpdate.DateOfBirth = request.DateOfBirth.Value; // Используем .Value

            }



            // ИСПРАВЛЕНИЕ ЗДЕСЬ для EnrollmentDate

            if (request.EnrollmentDate.HasValue) // Проверяем, есть ли значение

            {

                studentToUpdate.EnrollmentDate = request.EnrollmentDate.Value; // Используем .Value

            }





            // Check if the new GroupId exists before assigning

            if (studentToUpdate.GroupId != request.GroupId) // Only check if group is actually changing

            {

                // GroupId в UpdateStudentRequest - это int, а не int?, так что GroupId всегда имеет значение.

                // Вам нужно решить, хотите ли вы разрешать клиенту установить GroupId в null через этот запрос.

                // Если GroupId в сущности Student является int? (nullable), и вы хотите разрешить null,

                // то UpdateStudentRequest.GroupId также должен быть int?.

                // В текущем сценарии GroupId в запросе не nullable, поэтому проверка request.GroupId != 0 (если 0 - это "нет группы")

                // или проверка на null в случае int? GroupId.



                var newGroupExists = await _context.Groups.AnyAsync(g => g.GroupId == request.GroupId);

                if (!newGroupExists)

                {

                    return false; // New GroupId does not exist

                }

                studentToUpdate.GroupId = request.GroupId;

            }



            try

            {

                await _context.SaveChangesAsync();

                return true;

            }

            catch (DbUpdateConcurrencyException)

            {

                // Check if the entity still exists to differentiate between concurrency and not found

                if (!_context.Students.Any(e => e.StudentId == studentId))

                {

                    return false; // Not found (deleted by another user or didn't exist initially)

                }

                throw; // Re-throw if it's a genuine concurrency issue

            }

            catch (Exception)

            {

                // Log the exception here

                return false; // Failed for other reasons

            }

        }



        /// <summary>

        /// Deletes a student and their associated user account.

        /// </summary>

        /// <param name="studentId">The ID of the student to delete.</param>

        /// <returns>True if the student and user were deleted, false if not found.</returns>

        public async Task<bool> DeleteStudentAsync(int studentId)

        {

            var studentToDelete = await _context.Students

              .Include(s => s.User) // Include User to delete it as well

                  .FirstOrDefaultAsync(s => s.StudentId == studentId);



            if (studentToDelete == null)

            {

                return false; // Student not found

            }




            // Important: Consider checking for dependencies before deleting (e.g., grades)

            // If you have cascade deletes configured in EF Core, it might handle related entities.

            // Otherwise, manually delete dependent entities or return false/throw error.

            var hasGrades = await _context.Grades.AnyAsync(g => g.StudentId == studentId);

            if (hasGrades)

            {

                // Optionally, you might want to prevent deletion or soft-delete.

                // For now, let's say we prevent if grades exist.

                return false; // Cannot delete student with associated grades.

            }



            _context.Students.Remove(studentToDelete);

            if (studentToDelete.User != null)

            {

                _context.Users.Remove(studentToDelete.User); // Also delete the associated user

            }



            await _context.SaveChangesAsync();

            return true;

        }



        // --- Teacher Management Methods ---



        /// <summary>

        /// Adds a new teacher, creating a new user account and linking it to a teacher profile.

        /// </summary>

        /// <param name="request">Data for the new teacher and their user account.</param>

        /// <returns>The DTO of the newly created teacher, or null if username/login already exists.</returns>

        public async Task<TeacherDto?> AddTeacherAsync(AddTeacherRequest request)

        {

            // 1. Check if the login (username) already exists

            if (await _context.Users.AnyAsync(u => u.Username == request.Login))

            {

                // Возможно, стоит выбросить исключение, а не возвращать null

                // throw new ArgumentException("Login already taken.");

                return null; // Login already taken

            }



            // 2. Найти RoleId для "Преподаватель"

            var teacherRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Преподаватель");

            if (teacherRole == null)

            {

                // Это критическая ошибка: роль "Преподаватель" должна существовать в базе данных

                throw new InvalidOperationException("Role 'Преподаватель' not found in the database. Please seed roles.");

            }



            // 3. Create the User entity

            var newUser = _mapper.Map<User>(request);

            newUser.RoleId = teacherRole.RoleId; // !!! ИСПРАВЛЕНО: Присваиваем RoleId для "Преподаватель"

            // newUser.UserType = "Преподаватель"; // УДАЛЕНО: Это свойство больше не существует

            // В реальном приложении: хешируйте newUser.PasswordHash здесь!

            // newUser.PasswordHash = _passwordHasher.HashPassword(request.Password); // Пример



            _context.Users.Add(newUser);

            await _context.SaveChangesAsync(); // Save to get the new User.Id



            // 4. Create the Teacher entity, linking it to the new User

            var newTeacher = _mapper.Map<Teacher>(request); // Maps any common fields (currently none for Teacher)

            newTeacher.UserId = newUser.Id; // Link to the newly created user



            _context.Teachers.Add(newTeacher);

            await _context.SaveChangesAsync();



            // 5. Load navigation properties for the DTO mapping

            // Как и ранее, если AutoMapper настроен правильно, эти LoadAsync могут быть не нужны.

            // Но они не повредят, если вы хотите быть уверенными.

            await _context.Entry(newTeacher)

            .Reference(t => t.User).LoadAsync();



            return _mapper.Map<TeacherDto>(newTeacher);

        }



        /// <summary>

        /// Retrieves a list of all teachers.

        /// </summary>

        /// <returns>A list of TeacherDto objects.</returns>

        public async Task<IEnumerable<TeacherDto>> GetAllTeachersAsync() // Changed List to IEnumerable
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .ToListAsync();
            return _mapper.Map<IEnumerable<TeacherDto>>(teachers);
        }



        /// <summary>

        /// Retrieves a specific teacher by their ID.

        /// </summary>

        /// <param name="teacherId">The ID of the teacher.</param>

        /// <returns>The DTO of the teacher if found, otherwise null.</returns>

        public async Task<TeacherDto?> GetTeacherDetailsByIdAsync(int teacherId)

        {

            var teacher = await _context.Teachers

              .Include(t => t.User)

              .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            return _mapper.Map<TeacherDto>(teacher);

        }




        /// <summary>

        /// Updates an existing teacher's details and their associated user account details.

        /// </summary>

        /// <param name="teacherId">The ID of the teacher to update.</param>

        /// <param name="request">The updated data for the teacher and their user account.</param>

        /// <returns>True if the teacher was updated, false if not found or no changes.</returns>

        public async Task<bool> UpdateTeacherAsync(int teacherId, UpdateTeacherRequest request)

        {

            var teacherToUpdate = await _context.Teachers

              .Include(t => t.User)

              .FirstOrDefaultAsync(t => t.TeacherId == teacherId);



            if (teacherToUpdate == null || teacherToUpdate.User == null)

            {

                return false; // Teacher or associated user not found

            }



            // --- Update User properties ---

            // Do NOT update Username (Login) or PasswordHash via this DTO,

            // as per your DTO's likely design.

            // If you need to update Username/Password, create separate methods for security reasons.



            // Assuming FullName in DTO needs to be parsed for FirstName/LastName

            var names = request.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            teacherToUpdate.User.FirstName = names.ElementAtOrDefault(0) ?? string.Empty;

            teacherToUpdate.User.LastName = names.ElementAtOrDefault(1) ?? string.Empty;



            teacherToUpdate.User.Email = request.Email;



            // --- Update Teacher-specific properties here if AddTeacherRequest had them ---

            // (e.g., teacherToUpdate.Specialization = request.Specialization;)

            // If UpdateTeacherRequest does not have any teacher-specific properties,

            // this section might remain empty or map properties from DTO if they exist.



            try

            {

                await _context.SaveChangesAsync();

                return true;

            }

            catch (DbUpdateConcurrencyException)

            {

                if (!_context.Teachers.Any(e => e.TeacherId == teacherId))

                {

                    return false; // Not found (deleted by another user or didn't exist initially)

                }

                throw; // Re-throw if it's a genuine concurrency issue

            }

            catch (Exception)

            {

                // Log the exception here

                return false; // Failed for other reasons

            }

        }





        /// <summary>

        /// Deletes a teacher and their associated user account.

        /// </summary>

        /// <param name="teacherId">The ID of the teacher to delete.</param>

        /// <returns>True if the teacher and user were deleted, false if not found.</returns>

        public async Task<bool> DeleteTeacherAsync(int teacherId)

        {

            var teacherToDelete = await _context.Teachers

              .Include(t => t.User) // Include User to delete it as well

                  .FirstOrDefaultAsync(t => t.TeacherId == teacherId);



            if (teacherToDelete == null)

            {

                return false; // Teacher not found

            }



            // Important: Consider checking for dependencies before deleting (e.g., assignments, grades)

            var hasAssignments = await _context.TeacherSubjectGroupAssignments.AnyAsync(tsga => tsga.TeacherId == teacherId);

            if (hasAssignments)

            {

                // Prevent deletion if the teacher has active assignments.

                return false;

            }



            _context.Teachers.Remove(teacherToDelete);

            if (teacherToDelete.User != null)

            {

                _context.Users.Remove(teacherToDelete.User); // Also delete the associated user

            }



            await _context.SaveChangesAsync();

            return true;

        }



        // --- Subject Management Methods ---



        /// <summary>

        /// Adds a new academic subject.

        /// </summary>

        /// <param name="request">The data for the new subject.</param>

        /// <returns>The DTO of the newly created subject, or null if a subject with the same name already exists.</returns>

        public async Task<SubjectDto?> AddSubjectAsync(AddSubjectRequest request)

        {

            var existingSubject = await _context.Subjects

              .AnyAsync(s => s.Name == request.Name);



            if (existingSubject)

            {

                return null; // Subject with this name already exists

            }



            var subject = _mapper.Map<Subject>(request);

            _context.Subjects.Add(subject);

            await _context.SaveChangesAsync();



            return _mapper.Map<SubjectDto>(subject);

        }



        /// <summary>

        /// Retrieves a list of all academic subjects.

        /// </summary>

        /// <returns>A list of SubjectDto objects.</returns>

        public async Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync() // Changed List to IEnumerable
        {
            var subjects = await _context.Subjects.ToListAsync();
            return _mapper.Map<IEnumerable<SubjectDto>>(subjects);
        }



        /// <summary>

        /// Retrieves a specific academic subject by its ID.

        /// </summary>

        /// <param name="subjectId">The ID of the subject.</param>

        /// <returns>The DTO of the subject if found, otherwise null.</returns>

        public async Task<SubjectDto?> GetSubjectByIdAsync(int subjectId)

        {

            var subject = await _context.Subjects.FindAsync(subjectId);

            return _mapper.Map<SubjectDto>(subject);

        }



        /// <summary>

        /// Updates an existing academic subject.

        /// </summary>

        /// <param name="subjectId">The ID of the subject to update.</param>

        /// <param name="request">The updated data for the subject.</param>

        /// <returns>True if the subject was updated, false if not found or no changes.</returns>

        public async Task<bool> UpdateSubjectAsync(int subjectId, UpdateSubjectRequest request) // Reusing AddSubjectRequest

        {

            var subjectToUpdate = await _context.Subjects.FindAsync(subjectId);



            if (subjectToUpdate == null)

            {

                return false; // Subject not found

            }



            // Check if updated name would conflict with another *existing* subject (excluding itself)

            var conflictExists = await _context.Subjects

            .AnyAsync(s => s.Name == request.Name && s.SubjectId != subjectId);



            if (conflictExists)

            {

                return false; // Conflict with another subject's name

            }



            subjectToUpdate.Name = request.Name;



            try

            {

                await _context.SaveChangesAsync();

                return true;

            }

            catch (DbUpdateConcurrencyException)

            {

                if (!_context.Subjects.Any(e => e.SubjectId == subjectId))

                {

                    return false; // Not found (deleted by another user)

                }

                throw;

            }

            catch (Exception)

            {

                return false; // Failed for other reasons

            }

        }



        /// <summary>

        /// Deletes an academic subject by its ID.

        /// </summary>

        /// <param name="subjectId">The ID of the subject to delete.</param>

        /// <returns>True if the subject was deleted, false if not found.</returns>

        public async Task<bool> DeleteSubjectAsync(int subjectId)

        {

            var subjectToDelete = await _context.Subjects.FindAsync(subjectId);

            if (subjectToDelete == null)

            {

                return false; // Subject not found

            }



            // Consider checking for associated assignments or grades before deleting

            // to prevent foreign key constraint errors or unwanted data loss.

            // if (await _context.TeacherSubjectGroupAssignments.AnyAsync(tsga => tsga.SubjectId == subjectId)) { return false; }

            // if (await _context.Grades.AnyAsync(g => g.SubjectId == subjectId)) { return false; }



            _context.Subjects.Remove(subjectToDelete);

            await _context.SaveChangesAsync();

            return true;

        }



        // --- Assignment Management Methods ---



        /// <summary>

        /// Adds a new teacher-subject-group assignment.

        /// </summary>

        /// <param name="request">The assignment data.</param>

        /// <returns>The DTO of the newly created assignment, or null if conflicts or entities not found.</returns>

        public async Task<TeacherSubjectGroupAssignmentDto?> AddAssignmentAsync(AddTeacherSubjectGroupAssignmentRequest request)

        {

            // Check for existing identical assignment

            var existingAssignment = await _context.TeacherSubjectGroupAssignments

            .AnyAsync(tsga => tsga.TeacherId == request.TeacherId &&

                     tsga.SubjectId == request.SubjectId &&

                     tsga.GroupId == request.GroupId &&

                     tsga.SemesterId == request.SemesterId);



            if (existingAssignment) return null; // Assignment already exists



            // Check if all related entities exist

            var teacherExists = await _context.Teachers.AnyAsync(t => t.TeacherId == request.TeacherId);

            var subjectExists = await _context.Subjects.AnyAsync(s => s.SubjectId == request.SubjectId);

            var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == request.GroupId);

            var semesterExists = await _context.Semesters.AnyAsync(s => s.SemesterId == request.SemesterId);



            if (!teacherExists || !subjectExists || !groupExists || !semesterExists) return null; // One or more related entities not found



            var assignment = _mapper.Map<TeacherSubjectGroupAssignment>(request);

            _context.TeacherSubjectGroupAssignments.Add(assignment);

            await _context.SaveChangesAsync();



            // Load navigation properties for the DTO mapping

            await _context.Entry(assignment)

            .Reference(tsga => tsga.Teacher).LoadAsync();

            await _context.Entry(assignment)

              .Reference(tsga => tsga.Subject).LoadAsync();

            await _context.Entry(assignment)

              .Reference(tsga => tsga.Group).LoadAsync();

            await _context.Entry(assignment)

              .Reference(tsga => tsga.Semester).LoadAsync();



            // Load User for Teacher if needed in DTO

            if (assignment.Teacher != null)

            {

                await _context.Entry(assignment.Teacher)

                  .Reference(t => t.User).LoadAsync();

            }



            return _mapper.Map<TeacherSubjectGroupAssignmentDto>(assignment);

        }

        /// <summary>

        /// Retrieves a specific assignment by its ID.

        /// </summary>

        /// <param name="assignmentId">The ID of the assignment.</param>

        /// <returns>The DTO of the assignment if found, otherwise null.</returns>

        public async Task<TeacherSubjectGroupAssignmentDto?> GetAssignmentByIdAsync(int assignmentId)

        {

            var assignment = await _context.TeacherSubjectGroupAssignments

              .Include(tsga => tsga.Teacher)

                .ThenInclude(t => t.User) // Include user details of the teacher

                      .Include(tsga => tsga.Subject)

              .Include(tsga => tsga.Group)

              .Include(tsga => tsga.Semester)

              .FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == assignmentId);



            return _mapper.Map<TeacherSubjectGroupAssignmentDto>(assignment);

        }



        /// <summary>

        /// Retrieves a list of all teacher-subject-group assignments.

        /// </summary>

        /// <returns>A list of TeacherSubjectGroupAssignmentDto objects.</returns>

        public async Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAllAssignmentsAsync() // Changed List to IEnumerable
        {
            var assignments = await _context.TeacherSubjectGroupAssignments
                .Include(tsga => tsga.Teacher)
                    .ThenInclude(t => t.User)
                .Include(tsga => tsga.Subject)
                .Include(tsga => tsga.Group)
                .Include(tsga => tsga.Semester)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TeacherSubjectGroupAssignmentDto>>(assignments);
        }


        /// <summary>

        /// Retrieves assignments for a specific teacher.

        /// </summary>

        /// <param name="teacherId">The ID of the teacher.</param>

        /// <returns>A list of TeacherSubjectGroupAssignmentDto objects.</returns>



        /// <summary>

        /// Updates an existing teacher-subject-group assignment.

        /// </summary>

        /// <param name="assignmentId">The ID of the assignment to update.</param>

        /// <param name="request">The updated data for the assignment.</param>

        /// <returns>True if the assignment was updated, false if not found or related entities are missing.</returns>

        public async Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateTeacherSubjectGroupAssignmentRequest request)

        {

            var assignmentToUpdate = await _context.TeacherSubjectGroupAssignments.FindAsync(assignmentId);



            if (assignmentToUpdate == null) return false;



            // Ensure all related entities exist for the update

            var teacherExists = await _context.Teachers.AnyAsync(t => t.TeacherId == request.TeacherId);

            var subjectExists = await _context.Subjects.AnyAsync(s => s.SubjectId == request.SubjectId);

            var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == request.GroupId);

            var semesterExists = await _context.Semesters.AnyAsync(s => s.SemesterId == request.SemesterId);



            if (!teacherExists || !subjectExists || !groupExists || !semesterExists) return false;



            _mapper.Map(request, assignmentToUpdate); // Map changes from DTO to entity



            try

            {

                await _context.SaveChangesAsync();

                return true;

            }

            catch (DbUpdateConcurrencyException)

            {

                if (!_context.TeacherSubjectGroupAssignments.Any(e => e.TeacherSubjectGroupAssignmentId == assignmentId))

                {

                    return false; // Not found (deleted by another user)

                }

                throw;

            }

            catch (Exception)

            {

                return false;

            }

        }



        /// <summary>

        /// Deletes a teacher-subject-group assignment by its ID.

        /// </summary>

        /// <param name="assignmentId">The ID of the assignment to delete.</param>

        /// <returns>True if the assignment was deleted, false if not found.</returns>

        public async Task<bool> DeleteAssignmentAsync(int assignmentId)

        {

            var assignmentToDelete = await _context.TeacherSubjectGroupAssignments.FindAsync(assignmentId);

            if (assignmentToDelete == null) return false;



            // Consider checking for dependent grades before deleting.



            _context.TeacherSubjectGroupAssignments.Remove(assignmentToDelete);

            await _context.SaveChangesAsync();

            return true;

        }



        // --- Authorization Methods ---



        // --- Assignment-Related Authorization Methods ---



        /// <summary>

        /// Checks if a user is authorized to view all assignments.

        /// Authorization rules: Administrators and Teachers can view all assignments.

        /// </summary>

        public async Task<bool> CanUserDeleteUserAsync(int currentUserId, int targetUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            return currentUserRole == "Администратор"; // Only administrators can delete users
        }

        public async Task<bool> CanUserViewAllAssignmentsAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель";

        }

        public async Task<bool> CanUserViewUsersByRoleAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            return currentUserRole == "Администратор";
        }

        public async Task<bool> CanUserViewAllUsersAsync(int currentUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            return currentUserRole == "Администратор"; // Only administrators can view all users
        }
        /// <summary>

        /// Checks if a user is authorized to view details of a specific assignment.

        /// Authorization rules: Administrators can view any assignment. Teachers can view their own assignments.

        /// </summary>

        public async Task<bool> CanUserViewAssignmentDetailsAsync(int currentUserId, int assignmentId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);



            if (currentUserRole == "Администратор") return true;



            var assignment = await _context.TeacherSubjectGroupAssignments

                           .FirstOrDefaultAsync(tsga => tsga.TeacherSubjectGroupAssignmentId == assignmentId);



            if (assignment == null) return false;



            if (currentUserRole == "Преподаватель")

            {

                var teacherProfile = await GetTeacherByIdAsync(currentUserId);

                return teacherProfile != null && assignment.TeacherId == teacherProfile.TeacherId;

            }



            return false;

        }



        /// <summary>

        /// Checks if a user is authorized to add an assignment.

        /// Authorization rules: Only Administrators can add assignments.

        /// </summary>

        public async Task<bool> CanUserAddAssignmentAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to update an assignment.

        /// Authorization rules: Only Administrators can update assignments.

        /// </summary>

        public async Task<bool> CanUserUpdateAssignmentAsync(int currentUserId, int assignmentId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to delete an assignment.

        /// Authorization rules: Only Administrators can delete assignments.

        /// </summary>

        public async Task<bool> CanUserDeleteAssignmentAsync(int currentUserId, int assignmentId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        // --- Grade-Related Authorization Methods ---



        /// <summary>

        /// Checks if a teacher is authorized to assign a grade.

        /// Authorization rules: Administrators can assign any grade. Teachers can assign grades if they are assigned to teach the student's group for that subject and semester.

        /// </summary>

        public async Task<bool> CanTeacherAssignGrade(int currentUserId, int studentId, int subjectId, int semesterId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);



            if (currentUserRole == "Администратор") return true;

            if (currentUserRole != "Преподаватель") return false;



            var teacherProfile = await GetTeacherByIdAsync(currentUserId);

            if (teacherProfile == null) return false;



            var studentProfile = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (studentProfile == null || !studentProfile.GroupId.HasValue) return false;



            // Check if the teacher is assigned to the student's group for this subject and semester

            bool isAssigned = await _context.TeacherSubjectGroupAssignments

        .AnyAsync(tsga => tsga.TeacherId == teacherProfile.TeacherId &&

                 tsga.SubjectId == subjectId &&

                 tsga.GroupId == studentProfile.GroupId.Value &&

                 tsga.SemesterId == semesterId);



            return isAssigned;

        }


        public async Task<bool> CanUserViewAllRolesAsync(int currentUserId)
        {
            // Get the current user's role
            var userRole = await _context.Users // Using _context
                                           .Where(u => u.Id == currentUserId)
                                           .Select(u => u.Role.Name) // Assuming Role has a 'Name' property
                                           .FirstOrDefaultAsync();

            // Only Administrators can view all roles
            return userRole == "Администратор";
        }

        /// <summary>

        /// Checks if a teacher is authorized to update a specific grade.

        /// Authorization rules: Administrators can update any grade. Teachers can update grades they originally assigned.

        /// </summary>

        public async Task<bool> CanUserViewRoleDetailsAsync(int currentUserId, int roleId)
        {
            // Get the current user's role
            var userRole = await _context.Users // Using _context for ApplicationDbContext
                                           .Where(u => u.Id == currentUserId)
                                           .Select(u => u.Role.Name) // Assuming User has a Role navigation property and Role has a Name
                                           .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(userRole))
            {
                return false; // User not found or no role assigned
            }

            // Define who can view specific role details:
            // Only Administrators can view any role details.
            if (userRole == "Администратор")
            {
                return true;
            }

            // Optional: If you wanted a user to be able to view details of *their own* role:
            // var currentUserRoleId = await _context.Users
            //                                      .Where(u => u.Id == currentUserId)
            //                                      .Select(u => u.RoleId) // Assuming User has RoleId FK
            //                                      .FirstOrDefaultAsync();
            // if (currentUserRoleId == roleId) {
            //     return true;
            // }

            return false; // Other roles are not allowed to view arbitrary role details
        }

        public async Task<bool> CanUserCreateRoleAsync(int currentUserId)
        {
            // Get the current user's role
            var userRole = await _context.Users // Using _context for ApplicationDbContext
                                           .Where(u => u.Id == currentUserId)
                                           .Select(u => u.Role.Name) // Assuming User has a Role navigation property and Role has a Name
                                           .FirstOrDefaultAsync();

            // Only Administrators can create new roles
            return userRole == "Администратор";
        }



        public async Task<bool> CanUserUpdateRoleAsync(int currentUserId, int roleId)
        {
            // Get the current user's role
            var userRole = await _context.Users // Using _context for ApplicationDbContext
                                           .Where(u => u.Id == currentUserId)
                                           .Select(u => u.Role.Name) // Assuming User has a Role navigation property and Role has a Name
                                           .FirstOrDefaultAsync();

            // Only Administrators can update roles
            if (userRole == "Администратор")
            {
                return true;
            }

            // Optional: More complex logic might allow a user to update *their own* role's description
            // (if that's even a concept in your system, e.g., a student updating their own profile).
            // But for general role management, it's usually admin-only.

            return false; // Other roles are not allowed to update roles
        }

        public async Task<bool> CanUserDeleteRoleAsync(int currentUserId, int roleId)
        {
            // Get the current user's role
            var userRole = await _context.Users // Using _context for ApplicationDbContext
                                           .Where(u => u.Id == currentUserId)
                                           .Select(u => u.Role.Name) // Assuming User has a Role navigation property and Role has a Name
                                           .FirstOrDefaultAsync();

            // Only Administrators can delete roles
            if (userRole != "Администратор")
            {
                return false;
            }

            // Additional business rule: Prevent deletion of critical system roles
            // (You might want to make these configurable or fetch them from a static list)
            var roleToDelete = await _context.Roles.FindAsync(roleId);

            if (roleToDelete != null)
            {
                var criticalRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Администратор",
                    "Преподаватель",
                    "Студент"
                    // Add any other core roles that should never be deleted
                };

                if (criticalRoles.Contains(roleToDelete.Name))
                {
                    return false; // Cannot delete a critical system role
                }
            }
            else
            {
                // If roleToDelete is null, it means the role doesn't exist,
                // so the user can't "delete" it (though this might be handled by the service layer).
                // For this authorization method, returning true here might imply "yes, if it existed, you could delete it"
                // but returning false indicates "no, you cannot delete a non-existent role".
                // Let's return true, assuming the RoleService will handle the NotFound.
                return true;
            }

            return true; // If user is Admin and role is not critical, allow deletion
        }

        public async Task<bool> CanUserViewSubjectDetailsAsync(int currentUserId, int subjectId)
        {
            // Get the current user's role
            var userRole = await _context.Users // Using _context for ApplicationDbContext
                                           .Where(u => u.Id == currentUserId)
                                           .Select(u => u.Role.Name) // Assuming User has a Role navigation property and Role has a Name
                                           .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(userRole))
            {
                return false; // User not found or no role assigned
            }

            // Authorization logic based on role:
            if (userRole == "Администратор")
            {
                return true; // Administrators can view any subject details
            }
            else if (userRole == "Преподаватель")
            {
                // Teachers can view subjects:
                // Option 1 (Simple): All teachers can view any subject in the system.
                // return true;

                // Option 2 (More granular): Teachers can only view subjects they are assigned to teach.
                bool isTeacherAssignedToSubject = await _context.TeacherSubjectGroupAssignments
                                                                 .AnyAsync(tsga => tsga.Teacher.UserId == currentUserId &&
                                                                                   tsga.SubjectId == subjectId);
                return isTeacherAssignedToSubject;
            }
            else if (userRole == "Студент")
            {
                // Students can view subjects:
                // Students should typically only view subjects they are actively enrolled in or have assignments for.
                bool isStudentEnrolledInSubject = await _context.Students
                    .Where(s => s.UserId == currentUserId)
                    .AnyAsync(s => s.Group != null && s.Group.TeacherSubjectGroupAssignments
                                     .Any(tsga => tsga.SubjectId == subjectId) ||
                                 s.Grades.Any(g => g.SubjectId == subjectId) ||
                                 s.Attendances.Any(a => a.TeacherSubjectGroupAssignment.SubjectId == subjectId));

                return isStudentEnrolledInSubject;
            }

            return false; // Other roles are not allowed to view subject details
        }

        public async Task<bool> CanTeacherUpdateGrade(int currentUserId, int gradeId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);



            if (currentUserRole == "Администратор") return true;

            if (currentUserRole != "Преподаватель") return false;



            var teacherProfile = await GetTeacherByIdAsync(currentUserId);

            if (teacherProfile == null) return false;



            var grade = await _context.Grades.FindAsync(gradeId);

            if (grade == null) return false;



            return grade.TeacherId == teacherProfile.TeacherId;

        }



        /// <summary>

        /// Checks if a teacher is authorized to delete a specific grade.

        /// Authorization rules: Administrators can delete any grade. Teachers can delete grades they originally assigned.

        /// </summary>

        public async Task<bool> CanTeacherDeleteGrade(int currentUserId, int gradeId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);



            if (currentUserRole == "Администратор") return true;

            if (currentUserRole != "Преподаватель") return false;



            var teacherProfile = await GetTeacherByIdAsync(currentUserId);

            if (teacherProfile == null) return false;



            var grade = await _context.Grades.FindAsync(gradeId);

            if (grade == null) return false;



            return grade.TeacherId == teacherProfile.TeacherId;

        }



        /// <summary>

        /// Checks if a user is authorized to view a specific grade.

        /// Authorization rules: Administrators can view any grade. Students can view their own grades. Teachers can view grades they assigned or grades of students in groups they teach for the relevant subject/semester.

        /// </summary>

        public async Task<bool> CanUserViewGrade(int currentUserId, int gradeId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);



            if (currentUserRole == "Администратор") return true;



            var grade = await _context.Grades.Include(g => g.Student).FirstOrDefaultAsync(g => g.GradeId == gradeId);

            if (grade == null) return false;



            if (currentUserRole == "Студент")

            {

                var studentProfile = await GetStudentByIdAsync(currentUserId);

                return studentProfile != null && grade.StudentId == studentProfile.StudentId;

            }



            if (currentUserRole == "Преподаватель")

            {

                var teacherProfile = await GetTeacherByIdAsync(currentUserId);

                if (teacherProfile == null) return false;



                // If the teacher assigned this grade

                if (grade.TeacherId == teacherProfile.TeacherId) return true;



                // If the teacher teaches the student's group for this subject and semester

                if (grade.Student != null && grade.Student.GroupId.HasValue)

                {

                    bool teachesStudentGroupForSubjectAndSemester = await _context.TeacherSubjectGroupAssignments

                      .AnyAsync(tsga => tsga.TeacherId == teacherProfile.TeacherId &&

                               tsga.GroupId == grade.Student.GroupId.Value &&

                               tsga.SubjectId == grade.SubjectId &&

                               tsga.SemesterId == grade.SemesterId);

                    return teachesStudentGroupForSubjectAndSemester;

                }

            }

            return false;

        }



        /// <summary>

        /// Checks if a teacher is authorized to view a specific student's grades.

        /// Authorization rules: Teachers can view grades of students whose group they are assigned to teach.

        /// </summary>

        public async Task<bool> CanTeacherViewStudentGrades(int teacherProfileId, int studentId)

        {

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null || !student.GroupId.HasValue) return false;



            bool hasAssignmentInStudentGroup = await _context.TeacherSubjectGroupAssignments

              .AnyAsync(tsga => tsga.TeacherId == teacherProfileId &&

                       tsga.GroupId == student.GroupId.Value);



            return hasAssignmentInStudentGroup;

        }



        // --- Group-Related Authorization Methods ---



        /// <summary>

        /// Checks if a user is authorized to add a group.

        /// Authorization rules: Only Administrators can add groups.

        /// </summary>

        public async Task<bool> CanUserAddGroupAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to update a group.

        /// Authorization rules: Only Administrators can update groups.

        /// </summary>

        public async Task<bool> CanUserUpdateGroupAsync(int currentUserId, int groupId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }





        /// <summary>

        /// Checks if a user is authorized to delete a group.

        /// Authorization rules: Only Administrators can delete groups.

        /// </summary>
        /// 
        public async Task<bool> CanUserUpdateUserAsync(int currentUserId, int targetUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            // Only administrators can update any user.
            // You might allow a user to update their own profile too (currentUserId == targetUserId).
            return currentUserRole == "Администратор" || currentUserId == targetUserId;
        }

        public async Task<bool> CanUserDeleteGroupAsync(int currentUserId, int groupId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to view a specific group.

        /// Authorization rules: Administrators and Teachers can view any group. Students can view their own group.

        /// </summary>

        public async Task<bool> CanUserViewGroupAsync(int currentUserId, int groupId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);



            if (currentUserRole == "Администратор" || currentUserRole == "Преподаватель")

            {

                return true;

            }



            if (currentUserRole == "Студент")

            {

                var studentProfile = await _context.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);

                return studentProfile != null && studentProfile.GroupId == groupId;

            }



            return false;

        }



        /// <summary>

        /// Checks if a user is authorized to view all groups.

        /// Authorization rules: Administrators and Teachers can view all groups.

        /// </summary>

        public async Task<bool> CanUserViewAllGroupsAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель";

        }



        // --- Semester-Related Authorization Methods ---



        /// <summary>

        /// Checks if a user is authorized to add a semester.

        /// Authorization rules: Only Administrators can add semesters.

        /// </summary>
        /// 
        public async Task<bool> CanUserViewUserDetailsAsync(int currentUserId, int targetUserId)
        {
            var currentUserRole = await GetUserRoleAsync(currentUserId);
            if (currentUserRole == "Администратор") return true;
            if (currentUserRole == "Преподаватель") return true; // Teachers can view all users? Or just students they teach?
            if (currentUserRole == "Студент") return currentUserId == targetUserId; // Student can only view their own details

            return false;
        }

        public async Task<bool> CanUserAddSemesterAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to update a semester.

        /// Authorization rules: Only Administrators can update semesters.

        /// </summary>

        public async Task<bool> CanUserUpdateSemesterAsync(int currentUserId, int semesterId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to delete a semester.

        /// Authorization rules: Only Administrators can delete semesters.

        /// </summary>

        public async Task<bool> CanUserDeleteSemesterAsync(int currentUserId, int semesterId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to view a specific semester.

        /// Authorization rules: All roles (Admin, Teacher, Student) can view semesters.

        /// </summary>

        public async Task<bool> CanUserViewSemesterAsync(int currentUserId, int semesterId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель" || currentUserRole == "Студент";

        }



        /// <summary>

        /// Checks if a user is authorized to view all semesters.

        /// Authorization rules: All roles (Admin, Teacher, Student) can view all semesters.

        /// </summary>

        public async Task<bool> CanUserViewAllSemestersAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель" || currentUserRole == "Студент";

        }

        public async Task<bool> CanUserViewSemesterDetailsAsync(int currentUserId, int semesterId)
        {
            // Get the current user's role
            var userRole = await _context.Users // Using _context based on your previous snippet
                                           .Where(u => u.Id == currentUserId)
                                           .Select(u => u.Role.Name)
                                           .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(userRole))
            {
                return false; // User not found or no role
            }

            // Define who can view semester details:
            // Administrators and Teachers can view any semester details.
            // Students can view semester details, especially for semesters they are involved in.
            if (userRole == "Администратор" || userRole == "Преподаватель")
            {
                return true;
            }
            else if (userRole == "Студент")
            {
                // For a student, you might want more granular control.
                // For example, they can only view semesters where they have enrollments or grades.
                // A basic check for now: allow any student to see any semester detail for simplicity.
                // More complex logic would involve:
                // bool isStudentInSemester = await _context.Students
                //     .Where(s => s.UserId == currentUserId)
                //     .AnyAsync(s => s.Grades.Any(g => g.SemesterId == semesterId) ||
                //                    s.Attendances.Any(a => a.TeacherSubjectGroupAssignment.SemesterId == semesterId) ||
                //                    s.Group.TeacherSubjectGroupAssignments.Any(tsga => tsga.SemesterId == semesterId));
                // return isStudentInSemester;
                return true; // Simple rule for now: any student can view semester details.
            }

            return false; // Other roles are not allowed
        }



        // --- Student-Related Authorization Methods ---



        /// <summary>

        /// Checks if a user is authorized to add a new student.

        /// Authorization rules: Only Administrators can add students.

        /// </summary>

        public async Task<bool> CanUserAddStudentAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to view all students.

        /// Authorization rules: Administrators and Teachers can view all students.

        /// </summary>

        public async Task<bool> CanUserViewAllStudentsAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель";

        }



        /// <summary>

        /// Checks if a user is authorized to view details of a specific student.

        /// Authorization rules: Administrators can view any student's details. Teachers can view students they are assigned to teach (via group assignment). Students can view their own details.

        /// </summary>

        public async Task<bool> CanUserViewStudentDetailsAsync(int currentUserId, int studentId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);



            if (currentUserRole == "Администратор") return true;



            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return false;



            // If the user is the student themselves

            if (currentUserRole == "Студент")

            {

                var studentProfile = await GetStudentByIdAsync(currentUserId);

                return studentProfile != null && studentProfile.StudentId == studentId;

            }



            // If the user is a teacher

            if (currentUserRole == "Преподаватель")

            {

                var teacherProfile = await GetTeacherByIdAsync(currentUserId);

                if (teacherProfile == null) return false;



                // Check if the teacher is assigned to the student's group

                if (student.GroupId.HasValue)

                {

                    bool teachesStudentGroup = await _context.TeacherSubjectGroupAssignments

                      .AnyAsync(tsga => tsga.TeacherId == teacherProfile.TeacherId &&

                               tsga.GroupId == student.GroupId.Value);

                    return teachesStudentGroup;

                }

            }

            return false;

        }



        /// <summary>

        /// Checks if a user is authorized to update a specific student.

        /// Authorization rules: Only Administrators can update any student.

        /// </summary>
        /// 
        public async Task<UserDto?> AuthenticateAsync(LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Login);

            // If user is not found, or password verification fails
            // Use the correct method name: VerifyHashedPassword
            // And provide the correct arguments: (user object, stored hashed password, provided plain password)
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) != PasswordVerificationResult.Success) // <--- CORRECTED LINE
            {
                return null; // User not found or incorrect password
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> CanUserUpdateStudentAsync(int currentUserId, int studentId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to delete a specific student.

        /// Authorization rules: Only Administrators can delete any student.

        /// </summary>

        public async Task<bool> CanUserDeleteStudentAsync(int currentUserId, int studentId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        // --- Teacher-Related Authorization Methods ---



        /// <summary>

        /// Checks if a user is authorized to add a new teacher.

        /// Authorization rules: Only Administrators can add teachers.

        /// </summary>

        public async Task<bool> CanUserAddTeacherAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to view all teachers.

        /// Authorization rules: Administrators and other teachers can view all teachers.

        /// </summary>

        public async Task<bool> CanUserViewAllTeachersAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель";

        }



        /// <summary>

        /// Checks if a user is authorized to view details of a specific teacher.

        /// Authorization rules: Administrators and other teachers can view any teacher's details.

        /// </summary>

        public async Task<bool> CanUserViewTeacherDetailsAsync(int currentUserId, int teacherId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            // Assuming teachers can view other teachers' profiles (e.g., for collaboration)

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель";

        }



        /// <summary>

        /// Checks if a user is authorized to update a specific teacher.

        /// Authorization rules: Only Administrators can update any teacher.

        /// </summary>

        public async Task<bool> CanUserUpdateTeacherAsync(int currentUserId, int teacherId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to delete a specific teacher.

        /// Authorization rules: Only Administrators can delete any teacher.

        /// </summary>

        public async Task<bool> CanUserDeleteTeacherAsync(int currentUserId, int teacherId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }





        // --- Subject-Related Authorization Methods ---



        /// <summary>

        /// Checks if a user is authorized to add a new subject.

        /// Authorization rules: Only Administrators can add subjects.

        /// </summary>

        public async Task<bool> CanUserAddSubjectAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to update a specific subject.

        /// Authorization rules: Only Administrators can update any subject.

        /// </summary>

        public async Task<bool> CanUserUpdateSubjectAsync(int currentUserId, int subjectId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }



        /// <summary>

        /// Checks if a user is authorized to delete a specific subject.

        /// Authorization rules: Only Administrators can delete any subject.

        /// </summary>

        public async Task<bool> CanUserDeleteSubjectAsync(int currentUserId, int subjectId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор";

        }


        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(request.OldPassword))
            {
                // ... (existing old password verification)
                if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword) != PasswordVerificationResult.Success)
                {
                    return false;
                }
            }

            // Хэширование нового пароля
            // CORRECTED LINE: Pass both the user object and the new password string
            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword); // <--- CORRECTED!

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == userId))
                {
                    return false;
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing password for user {userId}: {ex.Message}");
                return false;
            }
        }


        /// <summary>

        /// Checks if a user is authorized to view a specific subject.

        /// Authorization rules: All roles (Admin, Teacher, Student) can view subjects.

        /// </summary>

        public async Task<bool> CanUserViewSubjectAsync(int currentUserId, int subjectId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель" || currentUserRole == "Студент";

        }



        /// <summary>

        /// Checks if a user is authorized to view all subjects.

        /// Authorization rules: All roles (Admin, Teacher, Student) can view all subjects.

        /// </summary>

        public async Task<bool> CanUserViewAllSubjectsAsync(int currentUserId)

        {

            var currentUserRole = await GetUserRoleAsync(currentUserId);

            return currentUserRole == "Администратор" || currentUserRole == "Преподаватель" || currentUserRole == "Студент";

        }

    }

}

