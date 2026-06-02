using ContactSystem.Helpers;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ContactSystem.Repositories
{
    public class GroupContactsRepository : IGroupContactsRepository
    {
        private readonly IDatabaseHelper _db;
        private readonly ILogger<GroupContactsRepository> _logger;

        public GroupContactsRepository(IDatabaseHelper db, ILogger<GroupContactsRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> AddAsync(int groupId, int contactId)
        {
            const string sp = "dbo.sp_AddContactToGroup";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@GroupId",   SqlDbType.Int) { Value = groupId });
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = contactId });

            var result = await cmd.ExecuteScalarAsync();
            var ok = result != null && Convert.ToInt32(result) > 0;
            _logger.LogInformation(
                "Added contact {ContactId} to group {GroupId}: {Result}.",
                contactId, groupId, ok ? "ok" : "no-op");
            return ok;
        }

        public async Task<int> RemoveAsync(int groupId, int contactId)
        {
            const string sp = "dbo.sp_RemoveContactFromGroup";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@GroupId",   SqlDbType.Int) { Value = groupId });
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = contactId });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation(
                "Removed contact {ContactId} from group {GroupId}. Rows affected: {Rows}.",
                contactId, groupId, rows);
            return rows;
        }

        public async Task<IEnumerable<Contact>> GetContactsByGroupIdAsync(int groupId)
        {
            const string sp = "dbo.sp_GetContactsByGroupId";
            var list = new List<Contact>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@GroupId", SqlDbType.Int) { Value = groupId });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapContact(reader));
            }

            _logger.LogInformation("Retrieved {Count} contacts for GroupId {GroupId}.", list.Count, groupId);
            return list;
        }

        public async Task<IEnumerable<Group>> GetGroupsByContactIdAsync(int contactId)
        {
            const string sp = "dbo.sp_GetGroupsByContactId";
            var list = new List<Group>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = contactId });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapGroup(reader));
            }

            _logger.LogInformation("Retrieved {Count} groups for ContactId {ContactId}.", list.Count, contactId);
            return list;
        }

        private static Contact MapContact(SqlDataReader r) => new()
        {
            ContactId      = r.GetInt32(r.GetOrdinal("ContactId")),
            FirstName      = r.GetString(r.GetOrdinal("FirstName")),
            LastName       = r.GetString(r.GetOrdinal("LastName")),
            CountryCode    = r.GetString(r.GetOrdinal("CountryCode")),
            NationalNumber = r.GetString(r.GetOrdinal("NationalNumber")),
            PhoneNumber    = r.GetInt64(r.GetOrdinal("PhoneNumber")),
            ProjectId      = r.GetInt32(r.GetOrdinal("ProjectId")),
            IsSubscribed   = r.GetBoolean(r.GetOrdinal("IsSubscribed")),
            CreatedDate    = r.GetDateTime(r.GetOrdinal("CreatedDate")),
            UpdatedDate    = r.IsDBNull(r.GetOrdinal("UpdatedDate")) ? null : r.GetDateTime(r.GetOrdinal("UpdatedDate"))
        };

        private static Group MapGroup(SqlDataReader r) => new()
        {
            GroupId    = r.GetInt32(r.GetOrdinal("GroupId")),
            GroupName  = r.GetString(r.GetOrdinal("GroupName")),
            ProjectId  = r.GetInt32(r.GetOrdinal("ProjectId"))
        };
    }
}
