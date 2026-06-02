using ContactSystem.DTOs;
using ContactSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace ContactSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("CRUD operations for managing Group records.")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _service;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(IGroupService service, ILogger<GroupsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Get all groups.</summary>
        /// <remarks>Returns a list of every group in the database, newest first.</remarks>
        /// <response code="200">Groups retrieved successfully.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Retrieve all groups",
            Description = "Returns a list of every group currently stored in the database.",
            OperationId = "GetAllGroups")]
        [SwaggerResponse(StatusCodes.Status200OK, "Groups retrieved successfully.",
            typeof(ApiResponse<IEnumerable<GroupResponseDto>>))]
        public async Task<ActionResult<ApiResponse<IEnumerable<GroupResponseDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Get a group by id.</summary>
        /// <param name="id">Primary key of the group.</param>
        /// <response code="200">Group found.</response>
        /// <response code="404">Not Found.</response>
        [HttpGet("{id:int}")]
        [SwaggerOperation(
            Summary = "Retrieve a group by id",
            Description = "Returns a single group that matches the supplied id.",
            OperationId = "GetGroupById")]
        [SwaggerResponse(StatusCodes.Status200OK, "Group found.",
            typeof(ApiResponse<GroupResponseDto>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<GroupResponseDto>>> GetById(
            [SwaggerParameter("The group id.", Required = true)] int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Get all groups belonging to a project.</summary>
        /// <param name="projectId">Project id to filter by.</param>
        /// <response code="200">Groups retrieved successfully.</response>
        /// <response code="404">Project not Found.</response>
        [HttpGet("project/{projectId:int}")]
        [SwaggerOperation(
            Summary = "Retrieve all groups for a project",
            Description = "Returns every group whose projectId matches the supplied value.",
            OperationId = "GetGroupsByProjectId")]
        [SwaggerResponse(StatusCodes.Status200OK, "Groups retrieved successfully.",
            typeof(ApiResponse<IEnumerable<GroupResponseDto>>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Project not Found.")]
        public async Task<ActionResult<ApiResponse<IEnumerable<GroupResponseDto>>>> GetByProjectId(
            [SwaggerParameter("The project id to filter groups by.", Required = true)] int projectId)
        {
            var result = await _service.GetByProjectIdAsync(projectId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Create a new group.</summary>
        /// <remarks>
        /// GroupName is required (1-255 characters). ProjectId is optional — omit it
        /// (or send null) to create a project-less group. If a value is supplied, it
        /// must be a positive integer that references an existing project.
        /// </remarks>
        /// <param name="dto">Group payload.</param>
        /// <response code="201">Group created.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Project not Found.</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new group",
            Description = "Adds a new group record. GroupName is required and must be 1-255 characters. " +
                          "ProjectId is OPTIONAL — omit it (or send null) to create a project-less group. " +
                          "If a value is supplied, it must be a positive integer that references an existing project.",
            OperationId = "CreateGroup")]
        [SwaggerRequestExample(typeof(GroupCreateDto), typeof(CreateGroupExample))]
        [SwaggerResponse(StatusCodes.Status201Created, "Group created successfully.",
            typeof(ApiResponse<GroupResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Project not Found.")]
        public async Task<ActionResult<ApiResponse<GroupResponseDto>>> Create(
            [FromBody] GroupCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.GroupId }, result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Update an existing group (partial / PATCH-style).</summary>
        /// <remarks>
        /// Send only the fields you want to change — omitted fields are left untouched.
        /// The <c>id</c> in the URL is authoritative; do not send id in the body.
        /// At least one of <c>groupName</c>, <c>projectId</c> must be provided
        /// (an empty body returns 400). Only the row with the matching id is updated.
        /// </remarks>
        /// <param name="id">Group id to update (taken from URL, not body).</param>
        /// <param name="dto">Partial group payload — only included fields are updated.</param>
        /// <response code="200">Group updated.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Not Found.</response>
        [HttpPut("{id:int}")]
        [SwaggerOperation(
            Summary = "Update an existing group (partial update)",
            Description = "Updates only the fields that are present in the request body. " +
                          "Omitted fields keep their current database values. " +
                          "The URL id is authoritative; any id in the request body is ignored.",
            OperationId = "UpdateGroup")]
        [SwaggerRequestExample(typeof(GroupUpdateDto), typeof(UpdateGroupExample))]
        [SwaggerResponse(StatusCodes.Status200OK, "Group updated successfully.",
            typeof(ApiResponse<GroupResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<GroupResponseDto>>> Update(
            [SwaggerParameter("The group id (authoritative).", Required = true)] int id,
            [FromBody] GroupUpdateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Delete a group.</summary>
        /// <param name="id">Group id to delete.</param>
        /// <response code="200">Group deleted.</response>
        /// <response code="404">Not Found.</response>
        [HttpDelete("{id:int}")]
        [SwaggerOperation(
            Summary = "Delete a group",
            Description = "Permanently removes the group record identified by id.",
            OperationId = "DeleteGroup")]
        [SwaggerResponse(StatusCodes.Status200OK, "Group deleted successfully.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(
            [SwaggerParameter("The group id.", Required = true)] int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }
    }

    public class CreateGroupExample : IExamplesProvider<GroupCreateDto>
    {
        public GroupCreateDto GetExamples() => new()
        {
            GroupName = "Backend",
            // ProjectId is OPTIONAL — omit it (or send null) to create a project-less group.
            // Provide a value to attach the group to an existing project.
            // ProjectId = 1
        };
    }

    public class UpdateGroupExample : IExamplesProvider<GroupUpdateDto>
    {
        public GroupUpdateDto GetExamples() => new()
        {
            // Partial update example: only groupName is being changed.
            // All other fields are omitted and will be left untouched in the database.
            GroupName = "Backend-Renamed"
        };
    }
}
