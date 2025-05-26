// Path: StudentPerformance.Api/Controllers/GradesController.cs



using Microsoft.AspNetCore.Mvc; // Для ControllerBase, HttpGet, HttpPost, HttpPut, HttpDelete, FromQuery, FromBody, ProducesResponseType

using StudentPerformance.Api.Models.DTOs; // Для GradeDto, AddGradeRequest, UpdateGradeRequest

using StudentPerformance.Api.Services; // Для GradeService, UserService

using System.Collections.Generic; // Для List<T>

using System.Threading.Tasks; // Для Task

using Microsoft.AspNetCore.Authorization; // Для атрибута [Authorize]

using System.Security.Claims; // Для работы с Claims (получение ID пользователя из JWT токена)

using System; // Для исключений (UnauthorizedAccessException, ArgumentException, InvalidOperationException)



namespace StudentPerformance.Api.Controllers

{

    [ApiController] // Указывает, что этот контроллер обслуживает HTTP API запросы

    [Route("api/[controller]")] // Определяет базовый маршрут для контроллера (например, /api/Grades)

    [Authorize] // Применяет политику авторизации ко всем методам в этом контроллере.

    // Это означает, что для доступа к любому эндпоинту здесь, пользователь должен быть аутентифицирован.

    public class GradesController : ControllerBase

    {

        private readonly GradeService _gradeService; // Внедрение GradeService для выполнения операций с оценками

        private readonly UserService _userService;      // Внедрение UserService для выполнения проверок авторизации и получения данных пользователя



        // Конструктор контроллера: зависимости внедряются автоматически фреймворком

        public GradesController(GradeService gradeService, UserService userService)

        {

            _gradeService = gradeService;

            _userService = userService;

        }



        // Вспомогательный метод для безопасного извлечения ID текущего пользователя из JWT токена

        // Это централизованное место для получения userId из ClaimsPrincipal (User)

        private int GetCurrentUserId()

        {

            // ClaimTypes.NameIdentifier обычно содержит ID пользователя после аутентификации

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))

            {

                return userId;

            }

            // Если ID пользователя не найден или недействителен, выбрасываем исключение.

            // Это должно быть поймано middleware аутентификации/авторизации, но здесь для надежности.

            throw new UnauthorizedAccessException("User ID claim not found or is invalid.");

        }



        /// <summary>

        /// Получает список всех оценок, с возможностью фильтрации.

        /// Правила доступа:

        /// - **Администратор**: может просматривать любые оценки, используя любые фильтры.

        /// - **Преподаватель**: может просматривать свои оценки или оценки студентов, которым он преподает.

        /// - **Студент**: может просматривать только свои оценки.

        /// </summary>

        /// <param name="studentId">ID студента для фильтрации (опционально).</param>

        /// <param name="teacherId">ID преподавателя для фильтрации (опционально).</param>

        /// <param name="subjectId">ID предмета для фильтрации (опционально).</param>

        /// <param name="semesterId">ID семестра для фильтрации (опционально).</param>

        /// <returns>HTTP 200 OK со списком GradeDto, 401 Unauthorized, 403 Forbidden.</returns>

        [HttpGet]

        [ProducesResponseType(200, Type = typeof(List<GradeDto>))]

        [ProducesResponseType(401)] // Unauthorized

        [ProducesResponseType(403)] // Forbidden

        public async Task<IActionResult> GetAllGrades(

      [FromQuery] int? studentId = null,

      [FromQuery] int? teacherId = null,

      [FromQuery] int? subjectId = null,

      [FromQuery] int? semesterId = null)

        {

            var currentUserId = GetCurrentUserId(); // Получаем ID текущего пользователя

            var currentUserRole = await _userService.GetUserRoleAsync(currentUserId); // Получаем роль пользователя



            // Логика авторизации в зависимости от роли

            if (currentUserRole == "Администратор")

            {

                // Администратор может получить любые оценки с любыми фильтрами

                var grades = await _gradeService.GetAllGradesAsync(studentId, teacherId, subjectId, semesterId);

                return Ok(grades);

            }

            else if (currentUserRole == "Преподаватель")

            {

                var teacherProfile = await _userService.GetTeacherByIdAsync(currentUserId);

                if (teacherProfile == null)

                {

                    return Forbid("Teacher profile not found for this user."); // Преподавательский профиль не найден

                }



                // Преподаватель может видеть только свои оценки или оценки студентов, которым он преподает

                // Если в запросе указан teacherId, и он не совпадает с ID текущего преподавателя, доступ запрещен

                if (teacherId.HasValue && teacherId.Value != teacherProfile.TeacherId)

                {

                    return Forbid("Teachers are only allowed to view their own grades or grades they are authorized to view.");

                }



                // Проверяем, если был запрошен studentId, имеет ли текущий преподаватель право просматривать оценки этого студента

                if (studentId.HasValue)

                {

                    var canViewStudentGrades = await _userService.CanTeacherViewStudentGrades(teacherProfile.TeacherId, studentId.Value);

                    if (!canViewStudentGrades)

                    {

                        return Forbid($"Teacher is not authorized to view grades for student ID {studentId.Value}.");

                    }

                }



                // Фильтруем результаты по ID текущего преподавателя по умолчанию

                var grades = await _gradeService.GetAllGradesAsync(studentId, teacherProfile.TeacherId, subjectId, semesterId);

                return Ok(grades);

            }

            else if (currentUserRole == "Студент")

            {

                var studentProfile = await _userService.GetStudentByIdAsync(currentUserId);

                if (studentProfile == null)

                {

                    return Forbid("Student profile not found for this user."); // Студенческий профиль не найден

                }



                // Студент может просматривать только свои оценки. Игнорируем любой переданный studentId и используем ID текущего студента.

                var grades = await _gradeService.GetAllGradesAsync(studentProfile.StudentId, null, subjectId, semesterId);

                return Ok(grades);

            }



            // Для любых других ролей доступ запрещен

            return Forbid("Access denied for this role.");

        }



        /// <summary>

        /// Получает конкретную оценку по ее ID.

        /// Правила доступа:

        /// - **Администратор**: может просматривать любую оценку.

        /// - **Преподаватель**: может просматривать оценку, если он является преподавателем по связанному предмету и студенту, или если он является преподавателем, выставившим эту оценку.

        /// - **Студент**: может просматривать только свои оценки.

        /// </summary>

        /// <param name="id">Идентификатор оценки.</param>

        /// <returns>HTTP 200 OK с GradeDto, 401 Unauthorized, 403 Forbidden, 404 Not Found.</returns>

        [HttpGet("{id}")]

        [ProducesResponseType(200, Type = typeof(GradeDto))]

        [ProducesResponseType(401)]

        [ProducesResponseType(403)]

        [ProducesResponseType(404)]

        public async Task<IActionResult> GetGradeById(int id)

        {

            var currentUserId = GetCurrentUserId();



            // Делегируем детальную проверку авторизации на UserService

            // Этот метод в UserService должен учитывать роли и права доступа.

            var isAuthorized = await _userService.CanUserViewGrade(currentUserId, id);

            if (!isAuthorized)

            {

                return Forbid("You are not authorized to view this grade.");

            }



            var grade = await _gradeService.GetGradeByIdAsync(id);

            if (grade == null)

            {

                return NotFound($"Grade with ID {id} not found.");

            }

            return Ok(grade);

        }



        /// <summary>

        /// Добавляет новую оценку.

        /// Правила доступа: Только преподаватель, который авторизован выставлять оценки для данного студента, предмета и семестра.

        /// </summary>

        /// <param name="request">Данные для создания новой оценки.</param>

        /// <returns>HTTP 201 Created с созданной GradeDto, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 500 Internal Server Error.</returns>

        [HttpPost]

        [ProducesResponseType(201, Type = typeof(GradeDto))]

        [ProducesResponseType(400)] // Bad Request (например, неверные входные данные)

        [ProducesResponseType(401)] // Unauthorized (пользователь не аутентифицирован)

        [ProducesResponseType(403)] // Forbidden (пользователь аутентифицирован, но не имеет прав)

        [ProducesResponseType(500)] // Internal Server Error (неожиданные ошибки)

        public async Task<IActionResult> AddGrade([FromBody] AddGradeRequest request)

        {

            // Проверка валидности модели DTO (на основе атрибутов валидации в AddGradeRequest)

            if (!ModelState.IsValid)

            {

                return BadRequest(ModelState);

            }



            var currentUserId = GetCurrentUserId();



            try

            {

                // Вызываем метод сервиса для добавления оценки, передавая ID текущего пользователя для авторизации

                var newGrade = await _gradeService.AddGradeAsync(request, currentUserId);



                // Возвращаем 201 Created с URI нового ресурса и сам ресурс

                // nameof(GetGradeById) гарантирует, что имя метода контроллера будет взято корректно

                return CreatedAtAction(nameof(GetGradeById), new { id = newGrade!.GradeId }, newGrade);

            }

            catch (UnauthorizedAccessException ex)

            {

                // Если GradeService выбросил исключение UnauthorizedAccessException, это означает 403 Forbidden

                return Forbid(ex.Message);

            }

            catch (ArgumentException ex)

            {

                // Если GradeService выбросил ArgumentException (например, не найден студент/предмет/семестр), это 400 Bad Request

                return BadRequest(ex.Message);

            }

            catch (InvalidOperationException ex)

            {

                // Если GradeService выбросил InvalidOperationException (например, профиль преподавателя не найден), это 500 Internal Server Error

                return StatusCode(500, ex.Message);

            }

            catch (Exception ex)

            {

                // Логирование любого другого неожиданного исключения и возврат 500

                // В реальном приложении здесь должно быть подробное логирование.

                return StatusCode(500, "An unexpected error occurred: " + ex.Message);

            }

        }



        /// <summary>

        /// Обновляет существующую оценку.

        /// Правила доступа: Только преподаватель, который авторизован обновлять данную конкретную оценку.

        /// </summary>

        /// <param name="id">Идентификатор обновляемой оценки.</param>

        /// <param name="request">Данные для обновления оценки.</param>

        /// <returns>HTTP 204 No Content при успешном обновлении, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 500 Internal Server Error.</returns>

        [HttpPut("{id}")]

        [ProducesResponseType(204)] // No Content

        [ProducesResponseType(400)]

        [ProducesResponseType(401)]

        [ProducesResponseType(403)]

        [ProducesResponseType(404)] // Not Found

        [ProducesResponseType(500)]

        public async Task<IActionResult> UpdateGrade(int id, [FromBody] UpdateGradeRequest request)

        {

            if (!ModelState.IsValid)

            {

                return BadRequest(ModelState);

            }



            var currentUserId = GetCurrentUserId();



            try

            {

                // Вызываем метод сервиса для обновления оценки, передавая ID текущего пользователя для авторизации

                var updated = await _gradeService.UpdateGradeAsync(id, request, currentUserId);

                if (!updated)

                {

                    // Если метод сервиса вернул false, значит оценка не найдена

                    return NotFound($"Grade with ID {id} not found or no changes were made.");

                }

                return NoContent(); // 204 No Content для успешного обновления без возврата тела

            }

            catch (UnauthorizedAccessException ex)

            {

                return Forbid(ex.Message);

            }

            catch (ArgumentException ex)

            {

                return BadRequest(ex.Message);

            }

            catch (Exception ex)

            {

                // Логирование и возврат 500

                return StatusCode(500, "An unexpected error occurred: " + ex.Message);

            }

        }



        /// <summary>

        /// Удаляет оценку.

        /// Правила доступа: Только преподаватель, который авторизован удалять данную конкретную оценку.

        /// </summary>

        /// <param name="id">Идентификатор удаляемой оценки.</param>

        /// <returns>HTTP 204 No Content при успешном удалении, 401 Unauthorized, 403 Forbidden, 404 Not Found, 500 Internal Server Error.</returns>

        [HttpDelete("{id}")]

        [ProducesResponseType(204)] // No Content

        [ProducesResponseType(401)]

        [ProducesResponseType(403)]

        [ProducesResponseType(404)]

        [ProducesResponseType(500)]

        public async Task<IActionResult> DeleteGrade(int id)

        {

            var currentUserId = GetCurrentUserId();



            try

            {

                // Вызываем метод сервиса для удаления оценки, передавая ID текущего пользователя для авторизации

                var deleted = await _gradeService.DeleteGradeAsync(id, currentUserId);

                if (!deleted)

                {

                    // Если метод сервиса вернул false, значит оценка не найдена

                    return NotFound($"Grade with ID {id} not found.");

                }

                return NoContent(); // 204 No Content для успешного удаления

            }

            catch (UnauthorizedAccessException ex)

            {

                return Forbid(ex.Message);

            }

            catch (Exception ex)

            {

                // Логирование и возврат 500

                return StatusCode(500, "An unexpected error occurred: " + ex.Message);

            }

        }

    }

}