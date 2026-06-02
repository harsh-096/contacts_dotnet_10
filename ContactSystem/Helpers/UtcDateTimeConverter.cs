using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContactSystem.Helpers
{
    /// <summary>
    /// Serialises every <see cref="DateTime"/> in Indian Standard Time
    /// (UTC+05:30, no DST) with an explicit "+05:30" offset, and parses
    /// incoming strings back as <see cref="DateTimeKind.Utc"/>.
    ///
    /// Why this exists: SQL Server stores the value via SYSUTCDATETIME()
    /// (so the actual stored time IS UTC), but the DateTime we get back
    /// from SqlDataReader has DateTimeKind.Unspecified. System.Text.Json
    /// will then serialise it as "2026-06-02T10:30:00" — a value with no
    /// offset, which browsers parse as LOCAL time. The result is a
    /// silent shift on every client. Emitting the timestamp with an
    /// explicit "+05:30" offset in IST means:
    ///   * the JSON contract is timezone-explicit
    ///   * the wire value already reflects the wall-clock time the user
    ///     cares about (e.g. "17:42" is the time in India)
    ///   * clients in any timezone can still convert accurately because
    ///     the offset is unambiguous
    ///
    /// Change the <c>+05:30</c> offset token or replace
    /// <see cref="GetIstTimeZone"/> if you ever need a different display
    /// timezone.
    /// </summary>
    public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        private const string OffsetToken = "+05:30";

        private static readonly TimeZoneInfo IstTimeZone = GetIstTimeZone();

        private static TimeZoneInfo GetIstTimeZone()
        {
            // Cross-platform: prefer IANA ("Asia/Kolkata"); fall back to
            // Windows ("India Standard Time"). Both are UTC+05:30 with no
            // DST, so the wall-clock offset is identical.
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (string.IsNullOrEmpty(s)) return default;
            var parsed = DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            // The wire contract is a fully-qualified instant; force UTC so
            // any downstream comparison is correct.
            return DateTime.SpecifyKind(parsed.ToUniversalTime(), DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // SqlDataReader returns Unspecified; treat that as UTC because
            // every stored procedure populates CreatedDate / UpdatedDate
            // via SYSUTCDATETIME(). If a caller hands us a Local value,
            // convert it to UTC first.
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

            // Convert to IST wall-clock time and write with the +05:30
            // offset so the JSON is unambiguous and displays IST on clients.
            var ist = TimeZoneInfo.ConvertTimeFromUtc(utc, IstTimeZone);
            writer.WriteStringValue(
                ist.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + OffsetToken);
        }
    }

    public sealed class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
    {
        private const string OffsetToken = "+05:30";

        private static readonly TimeZoneInfo IstTimeZone = UtcDateTimeConverterGetIst();

        private static TimeZoneInfo UtcDateTimeConverterGetIst()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
        }

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            var s = reader.GetString();
            if (string.IsNullOrEmpty(s)) return null;
            var parsed = DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return DateTime.SpecifyKind(parsed.ToUniversalTime(), DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }
            var utc = value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };
            var ist = TimeZoneInfo.ConvertTimeFromUtc(utc, IstTimeZone);
            writer.WriteStringValue(
                ist.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + OffsetToken);
        }
    }
}
