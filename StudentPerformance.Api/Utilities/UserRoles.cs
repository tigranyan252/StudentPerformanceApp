// Path: StudentPerformance.Api/Utilities/UserRoles.cs (или где вы создали файл)

namespace StudentPerformance.Api.Utilities // <--- Это пространство имен должно соответствовать пути файла
{
    public static class UserRoles
    {
        public const string Administrator = "Admin";   // <--- ИЗМЕНЕНО
        public const string Teacher = "Teacher";       // <--- ИЗМЕНЕНО
        public const string Student = "Student";
    }
}