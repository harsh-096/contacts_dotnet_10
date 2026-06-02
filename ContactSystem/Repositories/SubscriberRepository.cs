using ContactSystem.Helpers;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ContactSystem.Repositories
{
    public class SubscriberRepository : ISubscriberRepository
    {
        private readonly IDatabaseHelper _db;
        private readonly ILogger<SubscriberRepository> _logger;

        public SubscriberRepository(IDatabaseHelper db, ILogger<SubscriberRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<Subscriber>> GetAllAsync()
        {
            const string sp = "dbo.sp_GetAllSubscribers";
            var list = new List<Subscriber>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            _logger.LogInformation("Retrieved {Count} subscribers.", list.Count);
            return list;
        }

        public async Task<Subscriber?> GetByIdAsync(int id)
        {
            const string sp = "dbo.sp_GetSubscriberById";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Map(reader);
            }
            return null;
        }

        public async Task<int> CreateAsync(Subscriber subscriber)
        {
            const string sp = "dbo.sp_CreateSubscriber";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@FirstName",      SqlDbType.NVarChar,  50) { Value = subscriber.FirstName });
            cmd.Parameters.Add(new SqlParameter("@LastName",       SqlDbType.NVarChar,  50) { Value = subscriber.LastName });
            cmd.Parameters.Add(new SqlParameter("@CountryCode",    SqlDbType.NVarChar,   5) { Value = subscriber.CountryCode });
            cmd.Parameters.Add(new SqlParameter("@NationalNumber", SqlDbType.NVarChar,  20) { Value = subscriber.NationalNumber });
            cmd.Parameters.Add(new SqlParameter("@PhoneNumber",    SqlDbType.NVarChar,  25) { Value = subscriber.PhoneNumber });
            cmd.Parameters.Add(new SqlParameter("@IsSubscribed",   SqlDbType.Bit)          { Value = subscriber.IsSubscribed });

            var result = await cmd.ExecuteScalarAsync();
            var newId = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Created subscriber with Id {Id}.", newId);
            return newId;
        }

        public async Task<int> UpdateAsync(
            int id,
            string? firstName,
            string? lastName,
            string? countryCode,
            string? nationalNumber,
            string? phoneNumber,
            bool? isSubscribed)
        {
            const string sp = "dbo.sp_UpdateSubscriber";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@Id",             SqlDbType.Int)          { Value = id });
            cmd.Parameters.Add(new SqlParameter("@FirstName",      SqlDbType.NVarChar,  50) { Value = (object?)firstName      ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@LastName",       SqlDbType.NVarChar,  50) { Value = (object?)lastName       ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@CountryCode",    SqlDbType.NVarChar,   5) { Value = (object?)countryCode    ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@NationalNumber", SqlDbType.NVarChar,  20) { Value = (object?)nationalNumber ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@PhoneNumber",    SqlDbType.NVarChar,  25) { Value = (object?)phoneNumber    ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@IsSubscribed",   SqlDbType.Bit)          { Value = (object?)isSubscribed   ?? DBNull.Value });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation(
                "Updated subscriber Id {Id}. Rows affected: {Rows}. " +
                "Fields: firstName={F}, lastName={L}, countryCode={C}, nationalNumber={N}, phoneNumber={P}, isSubscribed={S}.",
                id, rows,
                firstName      ?? "(unchanged)",
                lastName       ?? "(unchanged)",
                countryCode    ?? "(unchanged)",
                nationalNumber ?? "(unchanged)",
                phoneNumber    ?? "(unchanged)",
                isSubscribed?.ToString() ?? "(unchanged)");
            return rows;
        }

        public async Task<int> DeleteAsync(int id)
        {
            const string sp = "dbo.sp_DeleteSubscriber";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Deleted subscriber Id {Id}. Rows affected: {Rows}.", id, rows);
            return rows;
        }

        public async Task<bool> PhoneNumberExistsAsync(string phoneNumber, int? excludeId = null)
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.Subscribers WHERE PhoneNumber = @PhoneNumber AND (@ExcludeId IS NULL OR Id <> @ExcludeId);";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 25) { Value = phoneNumber });
            cmd.Parameters.Add(new SqlParameter("@ExcludeId",   SqlDbType.Int)         { Value = (object?)excludeId ?? DBNull.Value });

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        private static Subscriber Map(SqlDataReader r) => new()
        {
            Id             = r.GetInt32(r.GetOrdinal("Id")),
            FirstName      = r.GetString(r.GetOrdinal("FirstName")),
            LastName       = r.GetString(r.GetOrdinal("LastName")),
            CountryCode    = r.GetString(r.GetOrdinal("CountryCode")),
            NationalNumber = r.GetString(r.GetOrdinal("NationalNumber")),
            PhoneNumber    = r.GetString(r.GetOrdinal("PhoneNumber")),
            IsSubscribed   = r.GetBoolean(r.GetOrdinal("IsSubscribed")),
            CreatedDate    = r.GetDateTime(r.GetOrdinal("CreatedDate")),
            UpdatedDate    = r.IsDBNull(r.GetOrdinal("UpdatedDate")) ? null : r.GetDateTime(r.GetOrdinal("UpdatedDate"))
        };
    }
}
