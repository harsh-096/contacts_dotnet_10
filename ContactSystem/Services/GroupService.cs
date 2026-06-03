using ContactSystem.DTOs;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.AspNetCore.Http;

namespace ContactSystem.Services
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _repo;
        private readonly IProjectRepository _projectRepo;
        private readonly IContactRepository _contactRepo;
        private readonly ILogger<GroupService> _logger;

        public GroupService(
            IGroupRepository repo,
            IProjectRepository projectRepo,
            IContactRepository contactRepo,
            ILogger<GroupService> logger)
        {
            _repo = repo;
            _projectRepo = projectRepo;
            _contactRepo = contactRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetAllAsync()
        {
            var groups = await _repo.GetAllAsync();
            return ApiResponse<IEnumerable<GroupResponseDto>>.Ok(groups.Select(ToDto));
        }

        public async Task<ApiResponse<GroupResponseDto>> GetByIdAsync(int id)
        {
            if (id <= 0)
                return ApiResponse<GroupResponseDto>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            var group = await _repo.GetByIdAsync(id);
            if (group is null)
                return ApiResponse<GroupResponseDto>.Fail($"Group with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            return ApiResponse<GroupResponseDto>.Ok(ToDto(group));
        }

        public async Task<ApiResponse<GroupResponseDto>> CreateAsync(GroupCreateDto dto)
        {
            if (dto.ProjectId <= 0)
                return ApiResponse<GroupResponseDto>.Fail("ProjectId is required.", statusCode: StatusCodes.Status400BadRequest);

            if (!await _projectRepo.ExistsAsync(dto.ProjectId))
                return ApiResponse<GroupResponseDto>.Fail(
                    $"Project with id {dto.ProjectId} not found.",
                    statusCode: StatusCodes.Status404NotFound);

            var entity = new Group
            {
                GroupName = dto.GroupName.Trim(),
                ProjectId = dto.ProjectId
            };

            var newId = await _repo.CreateAsync(entity);
            entity.GroupId = newId;

            var created = await _repo.GetByIdAsync(newId);
            return ApiResponse<GroupResponseDto>.Ok(ToDto(created!),
                $"Group '{entity.GroupName}' created successfully.");
        }

        public async Task<ApiResponse<GroupResponseDto>> UpdateAsync(int id, GroupUpdateDto dto)
        {
            if (id <= 0)
                return ApiResponse<GroupResponseDto>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            _logger.LogInformation("Updating group Id {Id}.", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<GroupResponseDto>.Fail($"Group with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            var newGroupName = dto.GroupName?.Trim();
            var newProjectId = dto.ProjectId;

            if (newProjectId is not null
                && newProjectId != existing.ProjectId
                && !await _projectRepo.ExistsAsync(newProjectId.Value))
            {
                return ApiResponse<GroupResponseDto>.Fail(
                    $"Project with id {newProjectId} not found.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var rows = await _repo.UpdateAsync(
                id: id,
                groupName: newGroupName,
                projectId: newProjectId);

            if (rows == 0)
            {
                _logger.LogWarning("Update affected 0 rows for Id {Id}.", id);
                return ApiResponse<GroupResponseDto>.Fail(
                    "Update failed; no rows affected.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation("Updated group Id {Id} ({Rows} row affected).", id, rows);

            var updated = await _repo.GetByIdAsync(id);
            return ApiResponse<GroupResponseDto>.Ok(ToDto(updated!), "Group updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            if (id <= 0)
                return ApiResponse<bool>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<bool>.Fail($"Group with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            try
            {
                var rows = await _repo.DeleteAsync(id);
                if (rows == 0)
                    return ApiResponse<bool>.Fail("Delete failed; no rows affected.", statusCode: StatusCodes.Status500InternalServerError);

                return ApiResponse<bool>.Ok(true, "Group deleted successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 547)
            {
                return ApiResponse<bool>.Fail(
                    "Group cannot be deleted because of dependent records.",
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        public async Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetByProjectIdAsync(int projectId)
        {
            if (projectId <= 0)
                return ApiResponse<IEnumerable<GroupResponseDto>>.Fail("Invalid projectId.", statusCode: StatusCodes.Status400BadRequest);

            if (!await _projectRepo.ExistsAsync(projectId))
                return ApiResponse<IEnumerable<GroupResponseDto>>.Fail(
                    $"Project with id {projectId} not found.",
                    statusCode: StatusCodes.Status404NotFound);

            var groups = await _repo.GetByProjectIdAsync(projectId);
            return ApiResponse<IEnumerable<GroupResponseDto>>.Ok(groups.Select(ToDto));
        }

        public async Task<ApiResponse<bool>> AddContactToGroupAsync(int groupId, int contactId)
        {
            if (groupId <= 0 || contactId <= 0)
                return ApiResponse<bool>.Fail("Invalid groupId or contactId.", statusCode: StatusCodes.Status400BadRequest);

            if (!await _contactRepo.ExistsAsync(contactId))
                return ApiResponse<bool>.Fail($"Contact with id {contactId} not found.", statusCode: StatusCodes.Status404NotFound);

            var group = await _repo.GetByIdAsync(groupId);
            if (group is null)
                return ApiResponse<bool>.Fail($"Group with id {groupId} not found.", statusCode: StatusCodes.Status404NotFound);

            try
            {
                var result = await _repo.AddContactToGroupAsync(groupId, contactId);
                return result
                    ? ApiResponse<bool>.Ok(true, "Contact added to group.")
                    : ApiResponse<bool>.Fail("Failed to add contact to group.", statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2627)
            {
                return ApiResponse<bool>.Fail("Contact is already a member of this group.", statusCode: StatusCodes.Status409Conflict);
            }
        }

        public async Task<ApiResponse<bool>> RemoveContactFromGroupAsync(int groupId, int contactId)
        {
            if (groupId <= 0 || contactId <= 0)
                return ApiResponse<bool>.Fail("Invalid groupId or contactId.", statusCode: StatusCodes.Status400BadRequest);

            var result = await _repo.RemoveContactFromGroupAsync(groupId, contactId);
            return result
                ? ApiResponse<bool>.Ok(true, "Contact removed from group.")
                : ApiResponse<bool>.Fail("Contact not found in this group.", statusCode: StatusCodes.Status404NotFound);
        }

        public async Task<ApiResponse<IEnumerable<ContactResponseDto>>> GetContactsByGroupIdAsync(int groupId)
        {
            if (groupId <= 0)
                return ApiResponse<IEnumerable<ContactResponseDto>>.Fail("Invalid groupId.", statusCode: StatusCodes.Status400BadRequest);

            var contacts = await _repo.GetContactsByGroupIdAsync(groupId);
            return ApiResponse<IEnumerable<ContactResponseDto>>.Ok(contacts.Select(ToContactDto));
        }

        public async Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetGroupsByContactIdAsync(int contactId)
        {
            if (contactId <= 0)
                return ApiResponse<IEnumerable<GroupResponseDto>>.Fail("Invalid contactId.", statusCode: StatusCodes.Status400BadRequest);

            if (!await _contactRepo.ExistsAsync(contactId))
                return ApiResponse<IEnumerable<GroupResponseDto>>.Fail(
                    $"Contact with id {contactId} not found.",
                    statusCode: StatusCodes.Status404NotFound);

            var groups = await _repo.GetGroupsByContactIdAsync(contactId);
            return ApiResponse<IEnumerable<GroupResponseDto>>.Ok(groups.Select(ToDto));
        }

        private static GroupResponseDto ToDto(Group g) => new()
        {
            GroupId   = g.GroupId,
            GroupName = g.GroupName,
            ProjectId = g.ProjectId
        };

        private static ContactResponseDto ToContactDto(Contact c) => new()
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
    }
}
