using ContactSystem.DTOs;

namespace ContactSystem.Interfaces
{
    public interface IProjectService
    {
        Task<ApiResponse<IEnumerable<ProjectResponseDto>>> GetAllAsync();
        Task<ApiResponse<ProjectResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<ProjectResponseDto>> CreateAsync(ProjectCreateDto dto);
        Task<ApiResponse<ProjectResponseDto>> UpdateAsync(int id, ProjectUpdateDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
