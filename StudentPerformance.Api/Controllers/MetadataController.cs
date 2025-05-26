// Path: Controllers/MetadataController.cs

using Microsoft.AspNetCore.Mvc;
using StudentPerformance.Api.Services; // <--- This line is crucial for finding ISemesterService etc.
using StudentPerformance.Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
        private readonly IAssignmentService _assignmentService; // For assignments data

        // Constructor for dependency injection
        public MetadataController(
            IGroupService groupService,
            ISubjectService subjectService,
            ISemesterService semesterService,
            IAssignmentService assignmentService) // Add assignmentService
        {
            _groupService = groupService;
            _subjectService = subjectService;
            _semesterService = semesterService;
            _assignmentService = assignmentService;
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
            var groupDtos = await _groupService.GetAllGroupsAsync(); // Call dedicated service
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
            var subjectDtos = await _subjectService.GetAllSubjectsAsync(); // Call dedicated service
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
            var semesterDtos = await _semesterService.GetAllSemestersAsync(); // Call dedicated service
            return Ok(semesterDtos);
        }

        /// <summary>
        /// Gets a list of all teacher-subject-group assignments.
        /// Requires Administrator role.
        /// </summary>
        /// <returns>A list of Assignment DTOs.</returns>
        [HttpGet("assignments")]
        [Authorize(Roles = "Администратор")] // Only administrator can see ALL assignments
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<TeacherSubjectGroupAssignmentDto>>> GetAllAssignments()
        {
            var assignmentDtos = await _assignmentService.GetAllAssignmentsAsync(); // Call dedicated service
            return Ok(assignmentDtos);
        }

        // You might also consider adding endpoints for roles, teachers, students, etc.
        // if they are also needed as metadata/lookup data.
    }
}