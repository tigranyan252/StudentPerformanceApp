using Bogus;
using StudentPerformance.Api.Data.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace StudentPerformance.Api.Data
{
    public static class DataSeeder
    {
        public static void SeedData(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            Role adminRole, teacherRole, studentRole;
            List<User> allUsers;
            List<User> teacherUsers;
            List<User> studentUsers;
            List<Group> groups;
            List<Subject> subjects;
            List<Semester> semesters;
            List<Student> students;
            List<Teacher> teachers;
            List<TeacherSubjectGroupAssignment> teacherSubjectGroupAssignments;
            List<Assignment> assignments;


            // 1. Seed Roles
            if (!context.Roles.Any())
            {
                var roles = new List<Role>
                {
                    new Role { Name = "Admin", Description = "Administrator", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Role { Name = "Teacher", Description = "Educator", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Role { Name = "Student", Description = "Enrolled Student", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                };
                context.Roles.AddRange(roles);
                context.SaveChanges();
            }
            // Retrieve roles by name since IDs are now database-generated
            adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            teacherRole = context.Roles.FirstOrDefault(r => r.Name == "Teacher");
            studentRole = context.Roles.FirstOrDefault(r => r.Name == "Student");

            // 2. Seed Users
            if (!context.Users.Any())
            {
                var userFaker = new Faker<User>()
                    // REMOVED: .RuleFor(u => u.Id, f => f.IndexFaker + 1) <--- REMOVE THIS LINE
                    .RuleFor(u => u.Username, f => f.Internet.UserName())
                    .RuleFor(u => u.PasswordHash, f => BCrypt.Net.BCrypt.HashPassword("password123"))
                    .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                    .RuleFor(u => u.LastName, f => f.Name.LastName())
                    .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
                    .RuleFor(u => u.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(u => u.UpdatedAt, f => f.Date.Recent(1));

                var adminUsersList = userFaker.Generate(1).Select(u => { u.RoleId = adminRole.RoleId; return u; }).ToList();
                var teacherUsersList = userFaker.Generate(3).Select(u => { u.RoleId = teacherRole.RoleId; return u; }).ToList();
                var studentUsersList = userFaker.Generate(20).Select(u => { u.RoleId = studentRole.RoleId; return u; }).ToList();

                context.Users.AddRange(adminUsersList);
                context.Users.AddRange(teacherUsersList);
                context.Users.AddRange(studentUsersList);
                context.SaveChanges();
            }
            allUsers = context.Users.ToList();
            teacherUsers = allUsers.Where(u => u.RoleId == teacherRole.RoleId).ToList();
            studentUsers = allUsers.Where(u => u.RoleId == studentRole.RoleId).ToList();


            // 3. Seed Groups
            if (!context.Groups.Any())
            {
                var groupFaker = new Faker<Group>()
                    // REMOVED: .RuleFor(g => g.GroupId, f => f.IndexFaker + 1) <--- REMOVE THIS LINE
                    .RuleFor(g => g.Name, f => $"Группа {f.Random.AlphaNumeric(3).ToUpper()}")
                    .RuleFor(g => g.YearOfStudy, f => f.Random.Int(1, 4))
                    .RuleFor(g => g.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(g => g.UpdatedAt, f => f.Date.Recent(1));
                context.Groups.AddRange(groupFaker.Generate(5));
                context.SaveChanges();
            }
            groups = context.Groups.ToList();

            // 4. Seed Subjects
            if (!context.Subjects.Any())
            {
                var subjectFaker = new Faker<Subject>()
                    // REMOVED: .RuleFor(s => s.SubjectId, f => f.IndexFaker + 1) <--- REMOVE THIS LINE
                    .RuleFor(s => s.Name, f => f.Commerce.ProductName())
                    .RuleFor(s => s.Code, f => f.Random.AlphaNumeric(5).ToUpper())
                    .RuleFor(s => s.Description, f => f.Lorem.Sentence())
                    .RuleFor(s => s.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(s => s.UpdatedAt, f => f.Date.Recent(1));
                context.Subjects.AddRange(subjectFaker.Generate(10));
                context.SaveChanges();
            }
            subjects = context.Subjects.ToList();

            // 5. Seed Semesters
            if (!context.Semesters.Any())
            {
                var semesterFaker = new Faker<Semester>()
                    // REMOVED: .RuleFor(s => s.SemesterId, f => f.IndexFaker + 1) <--- REMOVE THIS LINE
                    .RuleFor(s => s.Name, f => $"{f.Date.Recent(1).Year} {f.PickRandom("Весенний", "Осенний")}")
                    .RuleFor(s => s.StartDate, f => f.Date.Recent(1, DateTime.Now.AddMonths(-6)))
                    .RuleFor(s => s.EndDate, (f, s) => s.StartDate.AddMonths(4))
                    .RuleFor(s => s.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(s => s.UpdatedAt, f => f.Date.Recent(1));
                context.Semesters.AddRange(semesterFaker.Generate(4));
                context.SaveChanges();
            }
            semesters = context.Semesters.ToList();


            // 6. Seed Students (связываем с существующими Users и Groups)
            if (!context.Students.Any())
            {
                var generatedStudents = new List<Student>();
                var studentDataFaker = new Faker(); // Базовый Faker для генерации данных
                foreach (var user in studentUsers)
                {
                    // No need for usedStudentUserIds since UserId is a FK and not the PK of Student
                    generatedStudents.Add(new Student
                    {
                        UserId = user.Id,
                        GroupId = studentDataFaker.PickRandom(groups).GroupId,
                        DateOfBirth = studentDataFaker.Date.Past(20, DateTime.Now.AddYears(-18)),
                        EnrollmentDate = studentDataFaker.Date.Past(2, DateTime.Now.AddYears(-1)),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                context.Students.AddRange(generatedStudents);
                context.SaveChanges();
            }
            students = context.Students.ToList();


            // 7. Seed Teachers (связываем с существующими Users)
            if (!context.Teachers.Any())
            {
                var generatedTeachers = new List<Teacher>();
                var teacherDataFaker = new Faker(); // Базовый Faker для генерации данных
                foreach (var user in teacherUsers)
                {
                    // No need for usedTeacherUserIds
                    generatedTeachers.Add(new Teacher
                    {
                        UserId = user.Id,
                        Department = teacherDataFaker.Commerce.Department(),
                        Position = teacherDataFaker.Name.JobTitle(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                context.Teachers.AddRange(generatedTeachers);
                context.SaveChanges();
            }
            teachers = context.Teachers.ToList();


            // 8. Seed TeacherSubjectGroupAssignments
            if (!context.TeacherSubjectGroupAssignments.Any())
            {
                var assignmentFaker = new Faker<TeacherSubjectGroupAssignment>()
                    // REMOVED: .RuleFor(tsga => tsga.TeacherSubjectGroupAssignmentId, f => f.IndexFaker + 1) <--- REMOVE THIS LINE
                    .RuleFor(tsga => tsga.TeacherId, f => f.PickRandom(teachers).TeacherId)
                    .RuleFor(tsga => tsga.SubjectId, f => f.PickRandom(subjects).SubjectId)
                    .RuleFor(tsga => tsga.GroupId, f => f.PickRandom(groups).GroupId)
                    .RuleFor(tsga => tsga.SemesterId, f => f.PickRandom(semesters).SemesterId)
                    .RuleFor(tsga => tsga.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(tsga => tsga.UpdatedAt, f => f.Date.Recent(1));
                context.TeacherSubjectGroupAssignments.AddRange(assignmentFaker.Generate(20));
                context.SaveChanges();
            }
            teacherSubjectGroupAssignments = context.TeacherSubjectGroupAssignments.ToList();


            // 9. Seed Assignments
            if (!context.Assignments.Any())
            {
                var assignmentFaker = new Faker<Assignment>()
                    // REMOVED: .RuleFor(a => a.AssignmentId, f => f.IndexFaker + 1) <--- REMOVE THIS LINE
                    .RuleFor(a => a.TeacherSubjectGroupAssignmentId, f => f.PickRandom(teacherSubjectGroupAssignments).TeacherSubjectGroupAssignmentId)
                    .RuleFor(a => a.Title, f => f.Commerce.ProductName())
                    .RuleFor(a => a.Description, f => f.Lorem.Sentence())
                    .RuleFor(a => a.Type, f => f.PickRandom("Quiz", "Homework", "Project", "Exam"))
                    .RuleFor(a => a.MaxScore, f => f.Random.Decimal(5, 100))
                    .RuleFor(a => a.DueDate, f => f.Date.Future(1))
            .RuleFor(a => a.SubmissionDate, (f, a) => f.Date.Between(a.CreatedAt, a.DueDate))
                    .RuleFor(a => a.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(a => a.UpdatedAt, f => f.Date.Recent(1));
                context.Assignments.AddRange(assignmentFaker.Generate(50));
                context.SaveChanges();
            }
            assignments = context.Assignments.ToList();

            // 10. Seed Attendances
            if (!context.Attendances.Any())
            {
                var attendanceFaker = new Faker<Attendance>()
                    // REMOVED: .RuleFor(a => a.AttendanceId, f => f.IndexFaker + 1) <--- REMOVE THIS LINE
                    .RuleFor(a => a.StudentId, f => f.PickRandom(students).StudentId)
                    .RuleFor(a => a.TeacherSubjectGroupAssignmentId, f => f.PickRandom(teacherSubjectGroupAssignments).TeacherSubjectGroupAssignmentId)
                    .RuleFor(a => a.Date, f => f.Date.Recent(30))
                    .RuleFor(a => a.Status, f => f.PickRandom("Present", "Absent", "Late", "Excused"))
                    .RuleFor(a => a.Remarks, f => f.Lorem.Sentence(3))
                    .RuleFor(a => a.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(a => a.UpdatedAt, f => f.Date.Recent(1));
                context.Attendances.AddRange(attendanceFaker.Generate(100));
                context.SaveChanges();
            }

            // 11. Seed Grades
            if (!context.Grades.Any())
            {
                var baseFaker = new Faker();

                foreach (var student in students)
                {
                    var possibleAssignmentsForStudent = teacherSubjectGroupAssignments
                        .Where(tsga => tsga.GroupId == student.GroupId)
                        .ToList();

                    if (possibleAssignmentsForStudent.Any())
                    {
                        foreach (var tsga in possibleAssignmentsForStudent)
                        {
                            var subjectForGrade = subjects.FirstOrDefault(s => s.SubjectId == tsga.SubjectId);
                            var semesterForGrade = semesters.FirstOrDefault(sem => sem.SemesterId == tsga.SemesterId);
                            var teacherForGrade = teachers.FirstOrDefault(t => t.TeacherId == tsga.TeacherId);

                            for (int i = 0; i < baseFaker.Random.Int(1, 3); i++)
                            {
                                context.Grades.Add(new Grade
                                {
                                    // REMOVED: .RuleFor(g => g.GradeId, f => f.IndexFaker + 1) <--- REMOVE THIS LINE
                                    StudentId = student.StudentId,
                                    SubjectId = subjectForGrade?.SubjectId, // Now correctly nullable as per your model
                                    SemesterId = semesterForGrade?.SemesterId, // Now correctly nullable
                                    TeacherId = teacherForGrade?.TeacherId,
                                    TeacherSubjectGroupAssignmentId = tsga.TeacherSubjectGroupAssignmentId, // This is now correct since you added it to Grade.cs
                                    Value = baseFaker.Random.Decimal(0, 100),
                                    ControlType = baseFaker.PickRandom("Quiz", "Exam", "Lab", "Project"),
                                    DateReceived = baseFaker.Date.Recent(60),
                                    Status = baseFaker.PickRandom("Passed", "Failed", "Pending"),
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                }
                context.SaveChanges();
            }
        }
    }
}