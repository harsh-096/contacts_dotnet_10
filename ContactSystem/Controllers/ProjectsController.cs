using ContactSystem.DTOs;
using ContactSystem.DTOs.Examples;
using ContactSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace ContactSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("CRUD operations for managing Project records.")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IProjectService service, ILogger<ProjectsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Get all projects.</summary>
        /// <remarks>Returns a list of every project in the database, newest first.</remarks>
        /// <response code="200">Projects retrieved successfully.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Retrieve all projects",
            Description = "Returns a list of every project currently stored in the database, ordered by ProjectId descending.",
            OperationId = "GetAllProjects")]
        [SwaggerResponse(StatusCodes.Status200OK, "Projects retrieved successfully.",
            typeof(ApiResponse<IEnumerable<ProjectResponseDto>>))]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectResponseDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Get a project by id.</summary>
        /// <param name="id">Primary key of the project.</param>
        /// <response code="200">Project found.</response>
        /// <response code="404">Not Found.</response>
        [HttpGet("{id:int}")]
        [SwaggerOperation(
            Summary = "Retrieve a project by id",
            Description = "Returns a single project that matches the supplied id.",
            OperationId = "GetProjectById")]
        [SwaggerResponse(StatusCodes.Status200OK, "Project found.",
            typeof(ApiResponse<ProjectResponseDto>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<ProjectResponseDto>>> GetById(
            [SwaggerParameter("The project id (e.g. 1).", Required = true)] int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Create a new project.</summary>
        /// <param name="dto">Project payload.</param>
        /// <response code="201">Project created.</response>
        /// <response code="400">Bad Request.</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new project",
            Description = "Adds a new project record. ProjectName is required and must be 1-255 characters.",
            OperationId = "CreateProject")]
        [SwaggerRequestExample(typeof(ProjectCreateDto), typeof(CreateProjectExample))]
        [SwaggerRequestExample(typeof(ProjectCreateDto), typeof(CreateProjectExample_Alternative))]
        [SwaggerRequestExample(typeof(ProjectCreateDto), typeof(CreateProjectExample_Tech))]
        [SwaggerResponse(StatusCodes.Status201Created, "Project created successfully.",
            typeof(ApiResponse<ProjectResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        public async Task<ActionResult<ApiResponse<ProjectResponseDto>>> Create(
            [FromBody] ProjectCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.ProjectId }, result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Update an existing project (partial / PATCH-style).</summary>
        /// <remarks>
        /// Send only the fields you want to change — omitted fields are left untouched.
        /// The <c>id</c> in the URL is authoritative; do not send id in the body.
        /// At least one of <c>projectName</c> must be provided (an empty body returns 400).
        /// Only the row with the matching id is updated.
        /// </remarks>
        /// <param name="id">Project id to update (taken from URL, not body).</param>
        /// <param name="dto">Partial project payload — only included fields are updated.</param>
        /// <response code="200">Project updated.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Not Found.</response>
        [HttpPut("{id:int}")]
        [SwaggerOperation(
            Summary = "Update an existing project (partial update)",
            Description = "Updates only the fields that are present in the request body. " +
                          "Omitted fields keep their current database values. " +
                          "The URL id is authoritative; any id in the request body is ignored.",
            OperationId = "UpdateProject")]
        [SwaggerRequestExample(typeof(ProjectUpdateDto), typeof(UpdateProjectExample))]
        [SwaggerRequestExample(typeof(ProjectUpdateDto), typeof(UpdateProjectExample_NoChange))]
        [SwaggerResponse(StatusCodes.Status200OK, "Project updated successfully.",
            typeof(ApiResponse<ProjectResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<ProjectResponseDto>>> Update(
            [SwaggerParameter("The project id (authoritative).", Required = true)] int id,
            [FromBody] ProjectUpdateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Delete a project.</summary>
        /// <param name="id">Project id to delete.</param>
        /// <response code="200">Project deleted.</response>
        /// <response code="404">Not Found.</response>
        /// <response code="409">Project still has dependent subscribers or groups.</response>
        [HttpDelete("{id:int}")]
        [SwaggerOperation(
            Summary = "Delete a project",
            Description = "Permanently removes the project record identified by id. " +
                          "Returns 409 if the project still has subscribers or groups attached; " +
                          "remove them first.",
            OperationId = "DeleteProject")]
        [SwaggerResponse(StatusCodes.Status200OK, "Project deleted successfully.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Project still has dependent subscribers or groups.")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(
            [SwaggerParameter("The project id.", Required = true)] int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }
    }
}
