using ContactSystem.DTOs;
using ContactSystem.Models;

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

        Task<ApiResponse<bool>> AddContactToGroupAsync(int groupId, int contactId);
        Task<ApiResponse<bool>> RemoveContactFromGroupAsync(int groupId, int contactId);
        Task<ApiResponse<IEnumerable<ContactResponseDto>>> GetContactsByGroupIdAsync(int groupId);
        Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetGroupsByContactIdAsync(int contactId);
    }
}
