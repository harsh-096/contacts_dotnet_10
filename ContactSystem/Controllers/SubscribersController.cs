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
    [SwaggerTag("CRUD operations for managing Subscriber records.")]
    public class SubscribersController : ControllerBase
    {
        private readonly ISubscriberService _service;
        private readonly ILogger<SubscribersController> _logger;

        public SubscribersController(ISubscriberService service, ILogger<SubscribersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Get all subscribers.</summary>
        /// <remarks>Returns a list of every subscriber in the database, newest first.</remarks>
        /// <response code="200">Subscribers retrieved successfully.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Retrieve all subscribers",
            Description = "Returns a list of every subscriber currently stored in the database.",
            OperationId = "GetAllSubscribers")]
        [SwaggerResponse(StatusCodes.Status200OK, "Subscribers retrieved successfully.",
            typeof(ApiResponse<IEnumerable<SubscriberResponseDto>>))]
        public async Task<ActionResult<ApiResponse<IEnumerable<SubscriberResponseDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Get a subscriber by id.</summary>
        /// <param name="id">Primary key of the subscriber.</param>
        /// <response code="200">Subscriber found.</response>
        /// <response code="404">Not Found.</response>
        [HttpGet("{id:int}")]
        [SwaggerOperation(
            Summary = "Retrieve a subscriber by id",
            Description = "Returns a single subscriber that matches the supplied id.",
            OperationId = "GetSubscriberById")]
        [SwaggerResponse(StatusCodes.Status200OK, "Subscriber found.",
            typeof(ApiResponse<SubscriberResponseDto>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<SubscriberResponseDto>>> GetById(
            [SwaggerParameter("The subscriber id.", Required = true)] int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Create a new subscriber.</summary>
        /// <param name="dto">Subscriber payload.</param>
        /// <response code="201">Subscriber created.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="409">Already Exists Information.</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new subscriber",
            Description = "Adds a new subscriber record. PhoneNumber must be in E.164 international format (e.g. +919876543210, +12025551234) and unique.",
            OperationId = "CreateSubscriber")]
        [SwaggerRequestExample(typeof(CreateSubscriberDto), typeof(CreateSubscriberExample))]
        [SwaggerResponse(StatusCodes.Status201Created, "Subscriber created successfully.",
            typeof(ApiResponse<SubscriberResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Already Exists Information.")]
        public async Task<ActionResult<ApiResponse<SubscriberResponseDto>>> Create(
            [FromBody] CreateSubscriberDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return result.Success
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Update an existing subscriber (partial / PATCH-style).</summary>
        /// <remarks>
        /// Send only the fields you want to change — omitted fields are left untouched.
        /// The <c>id</c> in the URL is authoritative; do not send id in the body.
        /// At least one of <c>firstName</c>, <c>lastName</c>, <c>phoneNumber</c>, <c>isSubscribed</c> must be provided
        /// (an empty body returns 400). Only the row with the matching id is updated.
        /// </remarks>
        /// <param name="id">Subscriber id to update (taken from URL, not body).</param>
        /// <param name="dto">Partial subscriber payload — only included fields are updated.</param>
        /// <response code="200">Subscriber updated.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Not Found.</response>
        /// <response code="409">Already Exists Information.</response>
        [HttpPut("{id:int}")]
        [SwaggerOperation(
            Summary = "Update an existing subscriber (partial update)",
            Description = "Updates only the fields that are present in the request body. " +
                          "Omitted fields keep their current database values. " +
                          "The URL id is authoritative; any id in the request body is ignored.",
            OperationId = "UpdateSubscriber")]
        [SwaggerRequestExample(typeof(UpdateSubscriberDto), typeof(UpdateSubscriberExample))]
        [SwaggerResponse(StatusCodes.Status200OK, "Subscriber updated successfully.",
            typeof(ApiResponse<SubscriberResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad Request.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Already Exists Information.")]
        public async Task<ActionResult<ApiResponse<SubscriberResponseDto>>> Update(
            [SwaggerParameter("The subscriber id (authoritative).", Required = true)] int id,
            [FromBody] UpdateSubscriberDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }

        /// <summary>Delete a subscriber.</summary>
        /// <param name="id">Subscriber id to delete.</param>
        /// <response code="200">Subscriber deleted.</response>
        /// <response code="404">Not Found.</response>
        [HttpDelete("{id:int}")]
        [SwaggerOperation(
            Summary = "Delete a subscriber",
            Description = "Permanently removes the subscriber record identified by id.",
            OperationId = "DeleteSubscriber")]
        [SwaggerResponse(StatusCodes.Status200OK, "Subscriber deleted successfully.",
            typeof(ApiResponse<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found.")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(
            [SwaggerParameter("The subscriber id.", Required = true)] int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }
    }

    public class CreateSubscriberExample : IExamplesProvider<CreateSubscriberDto>
    {
        public CreateSubscriberDto GetExamples() => new()
        {
            FirstName    = "Mohan",
            LastName     = "Pyare",
            PhoneNumber  = "+919876543210",
            IsSubscribed = true
        };
    }

    public class UpdateSubscriberExample : IExamplesProvider<UpdateSubscriberDto>
    {
        public UpdateSubscriberDto GetExamples() => new()
        {
            // Partial update example: only firstName is being changed.
            // All other fields are omitted and will be left untouched in the database.
            FirstName    = "Mohan"
        };
    }
}
