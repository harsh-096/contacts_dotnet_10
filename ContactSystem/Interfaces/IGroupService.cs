using ContactSystem.DTOs;

namespace ContactSystem.Interfaces
{
    public interface IGroupService
    {
        Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetAllAsync();
        Task<ApiResponse<GroupResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<GroupResponseDto>> CreateAsync(GroupCreateDto dto);
        Task<ApiResponse<GroupResponseDto>> UpdateAsync(int id, GroupUpdateDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetByProjectIdAsync(int projectId);
    }
}
