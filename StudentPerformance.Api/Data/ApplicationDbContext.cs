// Path: StudentPerformance.Api/Data/ApplicationDbContext.cs

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using StudentPerformance.Api.Data.Entities;

namespace StudentPerformance.Api.Data
{
    // Наследуемся от DbContext из Entity Framework Core
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        // DbSet свойства для каждой сущности (они соответствуют таблицам в БД)
        // Через эти свойства вы будете получать доступ к данным таблиц
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<TeacherSubjectGroupAssignment> TeacherSubjectGroupAssignments { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Attendance> Attendances { get; set; }

        // Метод OnModelCreating используется для более детальной настройки модели,
        // которая не может быть полностью выражена с помощью атрибутов (Data Annotations).
        // Например, определение составных ключей, сложных связей, уникальных индексов и т.д.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- УНИКАЛЬНЫЕ ИНДЕКСЫ ---
            // Эти индексы гарантируют уникальность определенных полей или комбинаций полей в БД.

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique(); // Username должен быть уникальным

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique(); // Имя роли должно быть уникальным

            modelBuilder.Entity<Group>()
                .HasIndex(g => g.Name)
                .IsUnique(); // Имя группы должно быть уникальным

            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Name)
                .IsUnique(); // Имя предмета должно быть уникальным

            modelBuilder.Entity<Semester>()
                .HasIndex(s => s.Name)
                .IsUnique(); // Имя семестра должно быть уникальным

            modelBuilder.Entity<TeacherSubjectGroupAssignment>()
                .HasIndex(tsga => new { tsga.TeacherId, tsga.SubjectId, tsga.GroupId, tsga.SemesterId })
                .IsUnique(); // Преподаватель может быть назначен на один предмет в одной группе в одном семестре только один раз.

            modelBuilder.Entity<Grade>()
                .HasIndex(g => new { g.StudentId, g.AssignmentId })
                .IsUnique()
                .HasFilter("[AssignmentId] IS NOT NULL"); // Студент может иметь только одну оценку за конкретное задание.
                                                          // HasFilter для SQL Server: индекс применяется только когда AssignmentId не NULL.
                                                          // Для других баз данных может потребоваться другой синтаксис или подход.

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.StudentId, a.TeacherSubjectGroupAssignmentId, a.Date })
                .IsUnique(); // Студент может иметь только одну запись о посещаемости для конкретного занятия в определенную дату.

            // --- Настройка связей User и Role ---
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role) // У пользователя есть одна роль
                .WithMany(r => r.Users) // У роли может быть много пользователей
                .HasForeignKey(u => u.RoleId) // Внешний ключ находится в таблице Users по полю RoleId
                .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить роль, если на нее ссылаются пользователи

            // --- Настройка связей 1-к-1 между User и Student ---
            // UserId в Student является внешним ключом и первичным ключом
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Если удалить User, удаляется и Student (строгая 1-к-1 связь)

            // --- Настройка связей 1-к-1 между User и Teacher ---
            // UserId в Teacher является внешним ключом и первичным ключом
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Если удалить User, удаляется и Teacher (строгая 1-к-1 связь)

            // --- Настройка связей Group и Student ---
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Group) // У студента есть одна группа (может быть null)
                .WithMany(g => g.Students) // У группы может быть много студентов
                .HasForeignKey(s => s.GroupId)
                .IsRequired(false) // Явно указываем, что GroupId не обязателен
                .OnDelete(DeleteBehavior.SetNull); // Если удалить группу, GroupId у студентов станет NULL

            // --- Настройка связей TeacherSubjectGroupAssignment ---
            // Связь TeacherSubjectGroupAssignment с Teacher
            modelBuilder.Entity<TeacherSubjectGroupAssignment>()
                .HasOne(tsga => tsga.Teacher)
                .WithMany(t => t.TeacherSubjectGroupAssignments)
                .HasForeignKey(tsga => tsga.TeacherId)
                .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить учителя, если у него есть назначения

            // Связь TeacherSubjectGroupAssignment с Subject
            modelBuilder.Entity<TeacherSubjectGroupAssignment>()
                .HasOne(tsga => tsga.Subject)
                .WithMany(s => s.TeacherSubjectGroupAssignments)
                .HasForeignKey(tsga => tsga.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить предмет, если на него есть назначения

            // Связь TeacherSubjectGroupAssignment с Group
            modelBuilder.Entity<TeacherSubjectGroupAssignment>()
                .HasOne(tsga => tsga.Group)
                .WithMany(g => g.TeacherSubjectGroupAssignments)
                .HasForeignKey(tsga => tsga.GroupId)
                .IsRequired(false) // GroupId может быть null
                .OnDelete(DeleteBehavior.SetNull); // Если удалить группу, GroupId в назначении станет NULL

            // Связь TeacherSubjectGroupAssignment с Semester
            modelBuilder.Entity<TeacherSubjectGroupAssignment>()
                .HasOne(tsga => tsga.Semester)
                .WithMany(s => s.TeacherSubjectGroupAssignments)
                .HasForeignKey(tsga => tsga.SemesterId)
                .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить семестр, если на него есть назначения

            // --- Настройка связей Assignment ---
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.TeacherSubjectGroupAssignment)
                .WithMany(tsga => tsga.Assignments)
                .HasForeignKey(a => a.TeacherSubjectGroupAssignmentId)
                .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить назначение курса, если к нему привязаны задания

            // --- Настройка связей Grade ---
            // Grade с Student
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                // ИСПРАВЛЕНО: Изменено с Cascade на NoAction, чтобы избежать циклических или множественных каскадных путей
                .OnDelete(DeleteBehavior.NoAction);

            // Grade с Subject (опциональная прямая связь)
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Subject)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.SubjectId)
                .IsRequired(false) // SubjectId может быть null
                .OnDelete(DeleteBehavior.SetNull); // При удалении предмета, SubjectId у оценки станет NULL

            // Grade с Semester (опциональная прямая связь)
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Semester)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.SemesterId)
                .IsRequired(false) // SemesterId может быть null
                .OnDelete(DeleteBehavior.SetNull); // При удалении семестра, SemesterId у оценки станет NULL

            // Grade с Teacher (кто выставил оценку)
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Teacher)
                .WithMany(t => t.Grades)
                .HasForeignKey(g => g.TeacherId)
                .IsRequired(false) // TeacherId может быть null
                .OnDelete(DeleteBehavior.SetNull); // Если удалить учителя, TeacherId в оценке станет NULL

            // Grade с Assignment (опциональная прямая связь)
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Assignment)
                .WithMany(a => a.Grades)
                .HasForeignKey(g => g.AssignmentId)
                .IsRequired(false) // AssignmentId может быть null
                .OnDelete(DeleteBehavior.SetNull); // Если удалить задание, AssignmentId в оценке станет NULL

            // Grade с TeacherSubjectGroupAssignment (ОБЯЗАТЕЛЬНАЯ связь)
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.TeacherSubjectGroupAssignment)
                .WithMany(tsga => tsga.Grades)
                .HasForeignKey(g => g.TeacherSubjectGroupAssignmentId)
                .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить назначение курса, если к нему привязаны оценки

            // --- Настройка связей Attendance ---
            // Attendance с Student
            modelBuilder.Entity<Attendance>()
                .HasOne(att => att.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(att => att.StudentId)
                // ИСПРАВЛЕНО: Изменено с Cascade на NoAction, чтобы избежать циклических или множественных каскадных путей
                .OnDelete(DeleteBehavior.NoAction);

            // Attendance с TeacherSubjectGroupAssignment
            modelBuilder.Entity<Attendance>()
                .HasOne(att => att.TeacherSubjectGroupAssignment)
                .WithMany(tsga => tsga.Attendances)
                .HasForeignKey(att => att.TeacherSubjectGroupAssignmentId)
                .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить назначение курса, если к нему привязаны записи о посещаемости

            // Обязательно вызовите базовый метод OnModelCreating после ваших настроек!
            base.OnModelCreating(modelBuilder);
        }
    }
}
