// Path: StudentPerformance.Api/Controllers/MetadataController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using StudentPerformance.Api.Services.Interfaces; // Убедитесь, что этот namespace импортирован
using static StudentPerformance.Api.Utilities.UserRoles;

// Обычно, если UserRoles находится в StudentPerformance.Api.Utilities,
// то отдельный using StudentPerformance.Api.Services не нужен только для констант.
// using StudentPerformance.Api.Services; // Для доступа к константам ролей - можно убрать, если есть static using

namespace StudentPerformance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All actions here require authentication
    public class MetadataController : ControllerBase
    {
        // Inject dedicated services
        private readonly IGroupService _groupService;
        private readonly ISubjectService _subjectService;
        private readonly ISemesterService _semesterService;
        private readonly ITeacherSubjectGroupAssignmentService _assignmentService; // For assignments data
        // private readonly IUserService _userService; // Если понадобится для авторизации в этом контроллере

        // Constructor for dependency injection
        public MetadataController(
            IGroupService groupService,
            ISubjectService subjectService,
            ISemesterService semesterService,
            ITeacherSubjectGroupAssignmentService assignmentService
            /*, IUserService userService */) // Add assignmentService
        {
            _groupService = groupService;
            _subjectService = subjectService;
            _semesterService = semesterService;
            _assignmentService = assignmentService;
            // _userService = userService;
        }

        // --- Actions for retrieving lookup information (return DTOs) ---

        /// <summary>
        /// Gets a list of all groups.
        /// </summary>
        /// <returns>A list of Group DTOs.</returns>
        [HttpGet("groups")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<GroupDto>>> GetAllGroups()
        {
            // ИСПРАВЛЕНО: Передача null для name и code, чтобы получить все группы
            var groupDtos = await _groupService.GetAllGroupsAsync(null, null);
            return Ok(groupDtos);
        }

        /// <summary>
        /// Gets a list of all subjects (disciplines).
        /// </summary>
        /// <returns>A list of Subject DTOs.</returns>
        [HttpGet("subjects")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAllSubjects()
        {
            // ИСПРАВЛЕНО: Вызов GetAllSubjectsAsync с параметрами null, null
            var subjectDtos = await _subjectService.GetAllSubjectsAsync(null, null); // Call dedicated service
            return Ok(subjectDtos);
        }

        /// <summary>
        /// Gets a list of all semesters.
        /// </summary>
        /// <returns>A list of Semester DTOs.</returns>
        [HttpGet("semesters")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<SemesterDto>>> GetAllSemesters()
        {
            // ИСПРАВЛЕНО: Вызов GetAllSemestersAsync с параметром null для 'name'
            var semesterDtos = await _semesterService.GetAllSemestersAsync(null); // Call dedicated service
            return Ok(semesterDtos);
        }

        /// <summary>
        /// Gets a list of all teacher-subject-group assignments.
        /// Requires Administrator role.
        /// </summary>
        /// <returns>A list of Assignment DTOs.</returns>
        [HttpGet("assignments")]
        [Authorize(Roles = Administrator)] // ИСПРАВЛЕНО: Используем константу Administrator
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<TeacherSubjectGroupAssignmentDto>>> GetAllAssignments()
        {
            // ИСПРАВЛЕНО: Вызов GetAllAssignmentsAsync без параметров (предполагая, что он не требует фильтров)
            var assignmentDtos = await _assignmentService.GetAllAssignmentsAsync(); // Call dedicated service
            return Ok(assignmentDtos);
        }

        // You might also consider adding endpoints for roles, teachers, students, etc.
        // if they are also needed as metadata/lookup data.
    }
}
