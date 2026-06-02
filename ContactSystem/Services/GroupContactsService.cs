using ContactSystem.DTOs;
using ContactSystem.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ContactSystem.Services
{
    public class GroupContactsService : IGroupContactsService
    {
        private readonly IGroupContactsRepository _repo;
        private readonly IGroupRepository _groupRepo;
        private readonly IContactRepository _contactRepo;
        private readonly ILogger<GroupContactsService> _logger;

        public GroupContactsService(
            IGroupContactsRepository repo,
            IGroupRepository groupRepo,
            IContactRepository contactRepo,
            ILogger<GroupContactsService> logger)
        {
            _repo = repo;
            _groupRepo = groupRepo;
            _contactRepo = contactRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> AddContactToGroupAsync(int groupId, int contactId)
        {
            if (groupId <= 0 || contactId <= 0)
                return ApiResponse<bool>.Fail("Invalid groupId or contactId.", statusCode: StatusCodes.Status400BadRequest);

            var group = await _groupRepo.GetByIdAsync(groupId);
            if (group is null)
                return ApiResponse<bool>.Fail($"Group with id {groupId} not found.", statusCode: StatusCodes.Status404NotFound);

            var contact = await _contactRepo.GetByIdAsync(contactId);
            if (contact is null)
                return ApiResponse<bool>.Fail($"Contact with id {contactId} not found.", statusCode: StatusCodes.Status404NotFound);

            // The same-project rule is also enforced in the stored procedure
            // (sp_AddContactToGroup), but checking it here produces a friendlier
            // error message and avoids a round-trip on the obvious failure cases.
            if (group.ProjectId != contact.ProjectId)
            {
                return ApiResponse<bool>.Fail(
                    $"Contact (projectId={contact.ProjectId}) and group (projectId={group.ProjectId}) must belong to the same project.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            try
            {
                var ok = await _repo.AddAsync(groupId, contactId);
                if (!ok)
                {
                    return ApiResponse<bool>.Fail(
                        "Contact is already a member of the group.",
                        statusCode: StatusCodes.Status409Conflict);
                }

                return ApiResponse<bool>.Ok(true,
                    $"Contact {contactId} added to group {groupId} successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 50054)
            {
                return ApiResponse<bool>.Fail(
                    "Contact is already a member of the group.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 50053)
            {
                return ApiResponse<bool>.Fail(
                    "Contact and group must belong to the same project.",
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        public async Task<ApiResponse<bool>> RemoveContactFromGroupAsync(int groupId, int contactId)
        {
            if (groupId <= 0 || contactId <= 0)
                return ApiResponse<bool>.Fail("Invalid groupId or contactId.", statusCode: StatusCodes.Status400BadRequest);

            var group = await _groupRepo.GetByIdAsync(groupId);
            if (group is null)
                return ApiResponse<bool>.Fail($"Group with id {groupId} not found.", statusCode: StatusCodes.Status404NotFound);

            var contact = await _contactRepo.GetByIdAsync(contactId);
            if (contact is null)
                return ApiResponse<bool>.Fail($"Contact with id {contactId} not found.", statusCode: StatusCodes.Status404NotFound);

            var rows = await _repo.RemoveAsync(groupId, contactId);
            if (rows == 0)
            {
                return ApiResponse<bool>.Fail(
                    "Contact is not a member of the group.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return ApiResponse<bool>.Ok(true,
                $"Contact {contactId} removed from group {groupId} successfully.");
        }

        public async Task<ApiResponse<IEnumerable<ContactResponseDto>>> GetContactsByGroupIdAsync(int groupId)
        {
            if (groupId <= 0)
                return ApiResponse<IEnumerable<ContactResponseDto>>.Fail("Invalid groupId.", statusCode: StatusCodes.Status400BadRequest);

            var group = await _groupRepo.GetByIdAsync(groupId);
            if (group is null)
                return ApiResponse<IEnumerable<ContactResponseDto>>.Fail(
                    $"Group with id {groupId} not found.",
                    statusCode: StatusCodes.Status404NotFound);

            var contacts = await _repo.GetContactsByGroupIdAsync(groupId);
            return ApiResponse<IEnumerable<ContactResponseDto>>.Ok(contacts.Select(ToDto));
        }

        public async Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetGroupsByContactIdAsync(int contactId)
        {
            if (contactId <= 0)
                return ApiResponse<IEnumerable<GroupResponseDto>>.Fail("Invalid contactId.", statusCode: StatusCodes.Status400BadRequest);

            var contact = await _contactRepo.GetByIdAsync(contactId);
            if (contact is null)
                return ApiResponse<IEnumerable<GroupResponseDto>>.Fail(
                    $"Contact with id {contactId} not found.",
                    statusCode: StatusCodes.Status404NotFound);

            var groups = await _repo.GetGroupsByContactIdAsync(contactId);
            return ApiResponse<IEnumerable<GroupResponseDto>>.Ok(groups.Select(ToDto));
        }

        private static ContactResponseDto ToDto(Models.Contact c) => new()
        {
            ContactId      = c.ContactId,
            FirstName      = c.FirstName,
            LastName       = c.LastName,
            CountryCode    = c.CountryCode,
            NationalNumber = c.NationalNumber,
            PhoneNumber    = c.PhoneNumber,
            ProjectId      = c.ProjectId,
            IsSubscribed   = c.IsSubscribed,
            CreatedDate    = c.CreatedDate,
            UpdatedDate    = c.UpdatedDate
        };

        private static GroupResponseDto ToDto(Models.Group g) => new()
        {
            GroupId    = g.GroupId,
            GroupName  = g.GroupName,
            ProjectId  = g.ProjectId
        };
    }
}
