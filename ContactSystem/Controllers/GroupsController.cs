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
    [SwaggerTag("CRUD operations for managing Group records and the GroupContacts junction table.")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _service;
        private readonly IGroupContactsService _groupContactsService;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(
            IGroupService service,
            IGroupContactsService groupContactsService,
            ILogger<GroupsController> logger)
        {
            _service = service;
            _groupContactsService = groupContactsService;
            _logger = logger;
        }

        /// <summary>Get all groups.</summary>
        /// <remarks>Returns a list of every group in the database, newest first.</remarks>
        /// <response code="200">Groups retrieved successfully.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Retrieve all groups",
            Description = "Returns a list of every group currently stored in the database, ordered by GroupId descending.",
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
            [SwaggerParameter("The group id (e.g. 1).", Required = true)] int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Get the group (at most one) belonging to a project.</summary>
        /// <param name="projectId">Project id to filter by.</param>
        /// <response code="200">Group retrieved successfully (zero or one row).</response>
        /// <response code="404">Project not Found.</response>
        [HttpGet("project/{projectId:int}")]
        [SwaggerOperation(
            Summary = "Retrieve the group belonging to a project",
            Description = "Returns the (single) group whose projectId matches the supplied value. " +
                          "A project can have at most one group, so the response is either 0 or 1 row.",
            OperationId = "GetGroupByProjectId")]
        [SwaggerResponse(StatusCodes.Status200OK, "Group retrieved successfully.",
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

        /// <summary>Get all contacts that are members of a group.</summary>
        /// <param name="groupId">Group id.</param>
        /// <response code="200">Contacts retrieved successfully.</response>
        /// <response code="404">Group not Found.</response>
        [HttpGet("{groupId:int}/contacts")]
        [SwaggerOperation(
            Summary = "Retrieve all contacts of a group",
            Description = "Returns every contact that is a member of the supplied group, via the GroupContacts junction table.",
            OperationId = "GetContactsByGroupId")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contacts retrieved successfully.",
            typeof(ApiResponse<IEnumerable<ContactResponseDto>>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Group not Found.")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContactResponseDto>>>> GetContactsByGroupId(
            [SwaggerParameter("The group id.", Required = true)] int groupId)
        {
            var result = await _groupContactsService.GetContactsByGroupIdAsync(groupId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Create a new group.</summary>
        /// <remarks>
        /// GroupName is required (1-255 characters). ProjectId is required — a project can have
        /// only one group. The supplied ProjectId must reference an existing project and must
        /// not already be linked to a different group (409 Conflict otherwise).
        /// </remarks>
        /// <param name="dto">Group payload.</param>
        /// <response code="201">Group created.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Project not Found.</response>
        /// <response code="409">Conflict (project already has a group).</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new group",
            Description = "Adds a new group record. GroupName is required and must be 1-255 characters. " +
                          "ProjectId is REQUIRED and must reference an existing project. " +
                          "A project can have only one group — a second group for the same project is rejected with 409.",
            OperationId = "CreateGroup")]
        [SwaggerRequestExample(typeof(GroupCreateDto), typeof(CreateGroupExample))]
        [SwaggerRequestExample(typeof(GroupCreateDto), typeof(CreateGroupExample_Frontend))]
        [SwaggerRequestExample(typeof(GroupCreateDto), typeof(CreateGroupExample_Mobile))]
        [SwaggerResponse(StatusCodes.Status201Created, "Group created successfully.",
            typeof(ApiResponse<GroupResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Project not Found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Project already has a group.")]
        public async Task<ActionResult<ApiResponse<GroupResponseDto>>> Create(
            [FromBody] GroupCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.GroupId }, result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Add a contact to a group.</summary>
        /// <param name="groupId">Group id.</param>
        /// <param name="contactId">Contact id.</param>
        /// <response code="200">Contact added successfully.</response>
        /// <response code="404">Group or contact not Found.</response>
        /// <response code="409">Contact and group belong to different projects, or mapping already exists.</response>
        [HttpPost("{groupId:int}/contacts/{contactId:int}")]
        [SwaggerOperation(
            Summary = "Add a contact to a group",
            Description = "Creates a row in the GroupContacts junction table. " +
                          "The contact and the group MUST belong to the same project. " +
                          "Duplicate mappings are rejected with 409.",
            OperationId = "AddContactToGroup")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contact added to group successfully.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Group or contact not Found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Project mismatch or duplicate mapping.")]
        public async Task<ActionResult<ApiResponse<bool>>> AddContactToGroup(
            [SwaggerParameter("The group id.", Required = true)] int groupId,
            [SwaggerParameter("The contact id.", Required = true)] int contactId)
        {
            var result = await _groupContactsService.AddContactToGroupAsync(groupId, contactId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Update an existing group (partial / PATCH-style).</summary>
        /// <remarks>
        /// Send only the fields you want to change — omitted fields are left untouched.
        /// The <c>id</c> in the URL is authoritative; do not send id in the body.
        /// At least one of <c>groupName</c>, <c>projectId</c> must be provided
        /// (an empty body returns 400). Only the row with the matching id is updated.
        /// Changing <c>projectId</c> to one that is already taken by a different group returns 409.
        /// </remarks>
        /// <param name="id">Group id to update (taken from URL, not body).</param>
        /// <param name="dto">Partial group payload — only included fields are updated.</param>
        /// <response code="200">Group updated.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Not Found.</response>
        /// <response code="409">Conflict (target project already has a different group).</response>
        [HttpPut("{id:int}")]
        [SwaggerOperation(
            Summary = "Update an existing group (partial update)",
            Description = "Updates only the fields that are present in the request body. " +
                          "Omitted fields keep their current database values. " +
                          "The URL id is authoritative; any id in the request body is ignored.",
            OperationId = "UpdateGroup")]
        [SwaggerRequestExample(typeof(GroupUpdateDto), typeof(UpdateGroupExample))]
        [SwaggerRequestExample(typeof(GroupUpdateDto), typeof(UpdateGroupExample_MoveProject))]
        [SwaggerResponse(StatusCodes.Status200OK, "Group updated successfully.",
            typeof(ApiResponse<GroupResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Target project already has a different group.")]
        public async Task<ActionResult<ApiResponse<GroupResponseDto>>> Update(
            [SwaggerParameter("The group id (authoritative).", Required = true)] int id,
            [FromBody] GroupUpdateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Remove a contact from a group.</summary>
        /// <param name="groupId">Group id.</param>
        /// <param name="contactId">Contact id.</param>
        /// <response code="200">Contact removed successfully.</response>
        /// <response code="404">Group, contact, or mapping not Found.</response>
        [HttpDelete("{groupId:int}/contacts/{contactId:int}")]
        [SwaggerOperation(
            Summary = "Remove a contact from a group",
            Description = "Deletes the matching row from the GroupContacts junction table. " +
                          "Returns 404 if the contact is not currently a member of the group.",
            OperationId = "RemoveContactFromGroup")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contact removed from group successfully.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Group, contact, or mapping not Found.")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveContactFromGroup(
            [SwaggerParameter("The group id.", Required = true)] int groupId,
            [SwaggerParameter("The contact id.", Required = true)] int contactId)
        {
            var result = await _groupContactsService.RemoveContactFromGroupAsync(groupId, contactId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Delete a group.</summary>
        /// <param name="id">Group id to delete.</param>
        /// <response code="200">Group deleted.</response>
        /// <response code="404">Not Found.</response>
        /// <response code="409">Group still has contact members.</response>
        [HttpDelete("{id:int}")]
        [SwaggerOperation(
            Summary = "Delete a group",
            Description = "Permanently removes the group record identified by id. " +
                          "Returns 409 if the group still has contact members; remove them first.",
            OperationId = "DeleteGroup")]
        [SwaggerResponse(StatusCodes.Status200OK, "Group deleted successfully.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Group still has contact members.")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(
            [SwaggerParameter("The group id.", Required = true)] int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }
    }
}
