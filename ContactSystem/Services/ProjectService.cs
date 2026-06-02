using ContactSystem.DTOs;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.AspNetCore.Http;

namespace ContactSystem.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repo;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(IProjectRepository repo, ILogger<ProjectService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<ProjectResponseDto>>> GetAllAsync()
        {
            var projects = await _repo.GetAllAsync();
            return ApiResponse<IEnumerable<ProjectResponseDto>>.Ok(projects.Select(ToDto));
        }

        public async Task<ApiResponse<ProjectResponseDto>> GetByIdAsync(int id)
        {
            if (id <= 0)
                return ApiResponse<ProjectResponseDto>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            var project = await _repo.GetByIdAsync(id);
            if (project is null)
                return ApiResponse<ProjectResponseDto>.Fail($"Project with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            return ApiResponse<ProjectResponseDto>.Ok(ToDto(project));
        }

        public async Task<ApiResponse<ProjectResponseDto>> CreateAsync(ProjectCreateDto dto)
        {
            var entity = new Project
            {
                ProjectName = dto.ProjectName.Trim()
            };

            var newId = await _repo.CreateAsync(entity);
            entity.ProjectId = newId;

            var created = await _repo.GetByIdAsync(newId);
            return ApiResponse<ProjectResponseDto>.Ok(ToDto(created!),
                $"Project '{entity.ProjectName}' created successfully.");
        }

        public async Task<ApiResponse<ProjectResponseDto>> UpdateAsync(int id, ProjectUpdateDto dto)
        {
            if (id <= 0)
                return ApiResponse<ProjectResponseDto>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            _logger.LogInformation("Updating project Id {Id}.", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<ProjectResponseDto>.Fail($"Project with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            // Trim only the values that were actually provided.
            var newProjectName = dto.ProjectName?.Trim();

            var rows = await _repo.UpdateAsync(
                id: id,
                projectName: newProjectName);

            if (rows == 0)
            {
                _logger.LogWarning("Update affected 0 rows for Id {Id}.", id);
                return ApiResponse<ProjectResponseDto>.Fail(
                    "Update failed; no rows affected.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation("Updated project Id {Id} ({Rows} row affected).", id, rows);

            var updated = await _repo.GetByIdAsync(id);
            return ApiResponse<ProjectResponseDto>.Ok(ToDto(updated!), "Project updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            if (id <= 0)
                return ApiResponse<bool>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<bool>.Fail($"Project with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            var rows = await _repo.DeleteAsync(id);
            if (rows == 0)
                return ApiResponse<bool>.Fail("Delete failed; no rows affected.", statusCode: StatusCodes.Status500InternalServerError);

            return ApiResponse<bool>.Ok(true, "Project deleted successfully.");
        }

        private static ProjectResponseDto ToDto(Project p) => new()
        {
            ProjectId   = p.ProjectId,
            ProjectName = p.ProjectName
        };
    }
}
