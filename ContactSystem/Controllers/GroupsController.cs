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
    [SwaggerTag("CRUD operations for managing Group records. Groups belong to a single Project and can contain many Contacts.")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _service;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(
            IGroupService service,
            ILogger<GroupsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Retrieve all groups", OperationId = "GetAllGroups")]
        [SwaggerResponse(StatusCodes.Status200OK, "Groups retrieved successfully.",
            typeof(ApiResponse<IEnumerable<GroupResponseDto>>))]
        public async Task<ActionResult<ApiResponse<IEnumerable<GroupResponseDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [SwaggerOperation(Summary = "Retrieve a group by id", OperationId = "GetGroupById")]
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

        [HttpGet("project/{projectId:int}")]
        [SwaggerOperation(Summary = "Retrieve all groups of a project",
            Description = "Returns every group that belongs to the specified project.",
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

        [HttpPost]
        [SwaggerOperation(Summary = "Create a new group", OperationId = "CreateGroup")]
        [SwaggerRequestExample(typeof(GroupCreateDto), typeof(CreateGroupExample))]
        [SwaggerRequestExample(typeof(GroupCreateDto), typeof(CreateGroupExample_Frontend))]
        [SwaggerRequestExample(typeof(GroupCreateDto), typeof(CreateGroupExample_Mobile))]
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

        [HttpPut("{id:int}")]
        [SwaggerOperation(Summary = "Update an existing group (partial update)", OperationId = "UpdateGroup")]
        [SwaggerRequestExample(typeof(GroupUpdateDto), typeof(UpdateGroupExample))]
        [SwaggerRequestExample(typeof(GroupUpdateDto), typeof(UpdateGroupExample_MoveContact))]
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

        [HttpDelete("{id:int}")]
        [SwaggerOperation(Summary = "Delete a group", OperationId = "DeleteGroup")]
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

        [HttpGet("{groupId:int}/contacts")]
        [SwaggerOperation(Summary = "Get all contacts in a group", OperationId = "GetContactsByGroupId")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contacts retrieved.",
            typeof(ApiResponse<IEnumerable<ContactResponseDto>>))]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContactResponseDto>>>> GetContactsByGroupId(
            [SwaggerParameter("The group id.", Required = true)] int groupId)
        {
            var result = await _service.GetContactsByGroupIdAsync(groupId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        [HttpPost("{groupId:int}/contacts/{contactId:int}")]
        [SwaggerOperation(Summary = "Add a contact to a group", OperationId = "AddContactToGroup")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contact added to group.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Group or Contact not found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Contact already in group.")]
        public async Task<ActionResult<ApiResponse<bool>>> AddContactToGroup(
            [SwaggerParameter("The group id.", Required = true)] int groupId,
            [SwaggerParameter("The contact id.", Required = true)] int contactId)
        {
            var result = await _service.AddContactToGroupAsync(groupId, contactId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{groupId:int}/contacts/{contactId:int}")]
        [SwaggerOperation(Summary = "Remove a contact from a group", OperationId = "RemoveContactFromGroup")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contact removed from group.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Contact not in group.")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveContactFromGroup(
            [SwaggerParameter("The group id.", Required = true)] int groupId,
            [SwaggerParameter("The contact id.", Required = true)] int contactId)
        {
            var result = await _service.RemoveContactFromGroupAsync(groupId, contactId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }
    }
}
