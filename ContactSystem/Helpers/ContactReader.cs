using ContactSystem.Models;
using Microsoft.Data.SqlClient;

namespace ContactSystem.Helpers
{
    internal static class ContactReader
    {
        public static Contact Map(SqlDataReader r) => new()
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
    }
}
