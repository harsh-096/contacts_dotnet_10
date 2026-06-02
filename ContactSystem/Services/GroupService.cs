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
        private readonly IGroupContactsRepository _groupContactsRepo;
        private readonly ILogger<GroupService> _logger;

        public GroupService(
            IGroupRepository repo,
            IProjectRepository projectRepo,
            IGroupContactsRepository groupContactsRepo,
            ILogger<GroupService> logger)
        {
            _repo = repo;
            _projectRepo = projectRepo;
            _groupContactsRepo = groupContactsRepo;
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
            // ProjectId is required: a project can have only one group.
            if (dto.ProjectId <= 0)
                return ApiResponse<GroupResponseDto>.Fail("ProjectId is required.", statusCode: StatusCodes.Status400BadRequest);

            if (!await _projectRepo.ExistsAsync(dto.ProjectId))
                return ApiResponse<GroupResponseDto>.Fail(
                    $"Project with id {dto.ProjectId} not found.",
                    statusCode: StatusCodes.Status404NotFound);

            // Enforce "one project -> one group" at the service layer too (the DB
            // also enforces it via UQ_Groups_ProjectId).
            var existingForProject = await _repo.GetByProjectIdAsync(dto.ProjectId);
            if (existingForProject.Any())
            {
                var existing = existingForProject.First();
                return ApiResponse<GroupResponseDto>.Fail(
                    $"A group already exists for project {dto.ProjectId} (groupId={existing.GroupId}, groupName='{existing.GroupName}'). " +
                    "One project can have only one group.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var entity = new Group
            {
                GroupName  = dto.GroupName.Trim(),
                ProjectId  = dto.ProjectId
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

            // Trim only the values that were actually provided.
            var newGroupName = dto.GroupName?.Trim();
            var newProjectId = dto.ProjectId;

            // Project-existence check only runs when the project is being changed
            // AND a new value was supplied. Omitted projectId is a no-op.
            if (newProjectId is not null
                && newProjectId != existing.ProjectId
                && !await _projectRepo.ExistsAsync(newProjectId.Value))
            {
                return ApiResponse<GroupResponseDto>.Fail(
                    $"Project with id {newProjectId} not found.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            // Enforce "one project -> one group" when changing the project.
            if (newProjectId is not null && newProjectId != existing.ProjectId)
            {
                var conflicting = await _repo.GetByProjectIdAsync(newProjectId.Value);
                if (conflicting.Any())
                {
                    return ApiResponse<GroupResponseDto>.Fail(
                        $"A different group (groupId={conflicting.First().GroupId}) already exists for project {newProjectId}. " +
                        "One project can have only one group.",
                        statusCode: StatusCodes.Status409Conflict);
                }
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

            // Reject deletion if the group still has members so we surface a friendly
            // 409 instead of letting the FK violation bubble up as a 500.
            var memberCount = (await _groupContactsRepo.GetContactsByGroupIdAsync(id)).Count();
            if (memberCount > 0)
            {
                return ApiResponse<bool>.Fail(
                    $"Group with id {id} still has {memberCount} contact member(s). " +
                    "Remove all contacts from the group before deleting it.",
                    statusCode: StatusCodes.Status409Conflict);
            }

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
                    "Group cannot be deleted because it still has dependent contact members.",
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

        private static GroupResponseDto ToDto(Group g) => new()
        {
            GroupId    = g.GroupId,
            GroupName  = g.GroupName,
            ProjectId  = g.ProjectId
        };
    }
}
