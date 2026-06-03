using ContactSystem.Models;

namespace ContactSystem.Interfaces
{
    public interface IGroupRepository
    {
        Task<IEnumerable<Group>> GetAllAsync();
        Task<Group?> GetByIdAsync(int id);
        Task<int> CreateAsync(Group group);
        Task<int> UpdateAsync(int id, string? groupName, int? projectId);
        Task<int> DeleteAsync(int id);
        Task<IEnumerable<Group>> GetByProjectIdAsync(int projectId);
        Task<int> DeleteByProjectIdAsync(int projectId);

        Task<bool> AddContactToGroupAsync(int groupId, int contactId);
        Task<bool> RemoveContactFromGroupAsync(int groupId, int contactId);
        Task<IEnumerable<Contact>> GetContactsByGroupIdAsync(int groupId);
        Task<IEnumerable<Group>> GetGroupsByContactIdAsync(int contactId);
    }
}
