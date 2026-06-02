using ContactSystem.DTOs;

namespace ContactSystem.Interfaces
{
    public interface IGroupContactsService
    {
        Task<ApiResponse<bool>> AddContactToGroupAsync(int groupId, int contactId);
        Task<ApiResponse<bool>> RemoveContactFromGroupAsync(int groupId, int contactId);
        Task<ApiResponse<IEnumerable<ContactResponseDto>>> GetContactsByGroupIdAsync(int groupId);
        Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetGroupsByContactIdAsync(int contactId);
    }
}
