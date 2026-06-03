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
    [SwaggerTag("CRUD operations for managing Contact records.")]
    public class ContactsController : ControllerBase
    {
        private readonly IContactService _service;
        private readonly IGroupService _groupService;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(
            IContactService service,
            IGroupService groupService,
            ILogger<ContactsController> logger)
        {
            _service = service;
            _groupService = groupService;
            _logger = logger;
        }

        /// <summary>Get all contacts.</summary>
        /// <remarks>Returns a list of every contact in the database, newest first.</remarks>
        /// <response code="200">Contacts retrieved successfully.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Retrieve all contacts",
            Description = "Returns a list of every contact currently stored in the database, ordered by ContactId descending.",
            OperationId = "GetAllContacts")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contacts retrieved successfully.",
            typeof(ApiResponse<IEnumerable<ContactResponseDto>>))]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContactResponseDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Get a contact by id.</summary>
        /// <param name="id">Primary key of the contact.</param>
        /// <response code="200">Contact found.</response>
        /// <response code="404">Not Found.</response>
        [HttpGet("{id:int}")]
        [SwaggerOperation(
            Summary = "Retrieve a contact by id",
            Description = "Returns a single contact that matches the supplied id.",
            OperationId = "GetContactById")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contact found.",
            typeof(ApiResponse<ContactResponseDto>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<ContactResponseDto>>> GetById(
            [SwaggerParameter("The contact id (e.g. 1).", Required = true)] int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Get all contacts that belong to a specific project.</summary>
        /// <param name="projectId">Project id to filter by.</param>
        /// <response code="200">Contacts retrieved successfully.</response>
        /// <response code="404">Project not Found.</response>
        [HttpGet("project/{projectId:int}")]
        [SwaggerOperation(
            Summary = "Retrieve all contacts of a project",
            Description = "Returns every contact whose projectId matches the supplied value.",
            OperationId = "GetContactsByProjectId")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contacts retrieved successfully.",
            typeof(ApiResponse<IEnumerable<ContactResponseDto>>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Project not Found.")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContactResponseDto>>>> GetByProjectId(
            [SwaggerParameter("The project id to filter contacts by.", Required = true)] int projectId)
        {
            var result = await _service.GetByProjectIdAsync(projectId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Get every group a contact owns.</summary>
        /// <param name="contactId">Contact id.</param>
        /// <response code="200">Groups retrieved successfully.</response>
        /// <response code="404">Contact not Found.</response>
        [HttpGet("{contactId:int}/groups")]
        [SwaggerOperation(
            Summary = "Retrieve all groups a contact belongs to",
            Description = "Returns every group that the supplied contact is a member of via the GroupContacts junction.",
            OperationId = "GetGroupsByContactId")]
        [SwaggerResponse(StatusCodes.Status200OK, "Groups retrieved successfully.",
            typeof(ApiResponse<IEnumerable<GroupResponseDto>>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Contact not Found.")]
        public async Task<ActionResult<ApiResponse<IEnumerable<GroupResponseDto>>>> GetGroupsByContactId(
            [SwaggerParameter("The contact id.", Required = true)] int contactId)
        {
            var result = await _groupService.GetGroupsByContactIdAsync(contactId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Create a new contact.</summary>
        /// <param name="dto">Contact payload.</param>
        /// <response code="201">Contact created.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Project not Found.</response>
        /// <response code="409">Already Exists Information.</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new contact",
            Description = "Adds a new contact record. Supply CountryCode (e.g. +91) and NationalNumber (digits only, e.g. 9087648930); " +
                          "the server automatically builds PhoneNumber = CountryCode without '+' + NationalNumber (e.g. 919087648930) and enforces uniqueness on PhoneNumber. " +
                          "ProjectId is required and must reference an existing project.",
            OperationId = "CreateContact")]
        [SwaggerRequestExample(typeof(CreateContactDto), typeof(CreateContactExample))]
        [SwaggerRequestExample(typeof(CreateContactDto), typeof(CreateContactExample_US))]
        [SwaggerRequestExample(typeof(CreateContactDto), typeof(CreateContactExample_UAE))]
        [SwaggerResponse(StatusCodes.Status201Created, "Contact created successfully.",
            typeof(ApiResponse<ContactResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Project not Found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "PhoneNumber already exists.")]
        public async Task<ActionResult<ApiResponse<ContactResponseDto>>> Create(
            [FromBody] CreateContactDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.ContactId }, result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Update an existing contact (partial / PATCH-style).</summary>
        /// <remarks>
        /// Send only the fields you want to change — omitted fields are left untouched.
        /// The <c>id</c> in the URL is authoritative; do not send id in the body.
        /// At least one of <c>firstName</c>, <c>lastName</c>, <c>countryCode</c>, <c>nationalNumber</c>, <c>projectId</c>, <c>isSubscribed</c>
        /// must be provided (an empty body returns 400). If either <c>countryCode</c> or <c>nationalNumber</c> is supplied, the server
        /// automatically rebuilds <c>phoneNumber</c> from the merged values; clients never send phoneNumber.
        /// Only the row with the matching id is updated.
        /// </remarks>
        /// <param name="id">Contact id to update (taken from URL, not body).</param>
        /// <param name="dto">Partial contact payload — only included fields are updated.</param>
        /// <response code="200">Contact updated.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Not Found.</response>
        /// <response code="409">PhoneNumber already in use by another contact.</response>
        [HttpPut("{id:int}")]
        [SwaggerOperation(
            Summary = "Update an existing contact (partial update)",
            Description = "Updates only the fields that are present in the request body. " +
                          "Omitted fields keep their current database values. " +
                          "The URL id is authoritative; any id in the request body is ignored. " +
                          "If countryCode or nationalNumber is supplied, phoneNumber is recomputed server-side.",
            OperationId = "UpdateContact")]
        [SwaggerRequestExample(typeof(UpdateContactDto), typeof(UpdateContactExample))]
        [SwaggerRequestExample(typeof(UpdateContactDto), typeof(UpdateContactExample_ChangePhone))]
        [SwaggerRequestExample(typeof(UpdateContactDto), typeof(UpdateContactExample_Unsubscribe))]
        [SwaggerResponse(StatusCodes.Status200OK, "Contact updated successfully.",
            typeof(ApiResponse<ContactResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "PhoneNumber already in use by another contact.")]
        public async Task<ActionResult<ApiResponse<ContactResponseDto>>> Update(
            [SwaggerParameter("The contact id (authoritative).", Required = true)] int id,
            [FromBody] UpdateContactDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Delete a contact.</summary>
        /// <param name="id">Contact id to delete.</param>
        /// <response code="200">Contact deleted.</response>
        /// <response code="404">Not Found.</response>
        [HttpDelete("{id:int}")]
        [SwaggerOperation(
            Summary = "Delete a contact",
            Description = "Permanently removes the contact record identified by id. " +
                          "Junction entries in GroupContacts are cleaned up automatically.",
            OperationId = "DeleteContact")]
        [SwaggerResponse(StatusCodes.Status200OK, "Contact deleted successfully.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(
            [SwaggerParameter("The contact id.", Required = true)] int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }
    }
}
