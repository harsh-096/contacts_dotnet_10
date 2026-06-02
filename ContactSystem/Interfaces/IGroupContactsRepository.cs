using ContactSystem.Models;

namespace ContactSystem.Interfaces
{
    public interface IGroupContactsRepository
    {
        Task<bool> AddAsync(int groupId, int contactId);
        Task<int> RemoveAsync(int groupId, int contactId);
        Task<IEnumerable<Contact>> GetContactsByGroupIdAsync(int groupId);
        Task<IEnumerable<Group>> GetGroupsByContactIdAsync(int contactId);
    }
}
