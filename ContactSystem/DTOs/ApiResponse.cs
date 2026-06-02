using System.ComponentModel;

namespace ContactSystem.DTOs
{
    /// <summary>Uniform API response envelope used by every endpoint.</summary>
    /// <remarks>
    /// The shape is identical for success and failure: clients should branch on
    /// <see cref="Success"/> and inspect <see cref="StatusCode"/> if needed.
    /// </remarks>
    public class ApiResponse<T>
    {
        /// <summary>True when the request succeeded; false on any failure.</summary>
        [Description("True when the request succeeded; false on any failure (4xx / 5xx).")]
        public bool Success { get; set; }

        /// <summary>Human-readable summary of the outcome.</summary>
        [Description("Human-readable summary of the outcome.")]
        public string Message { get; set; } = string.Empty;

        /// <summary>Endpoint-specific payload; null on failure.</summary>
        [Description("Endpoint-specific payload. Null when Success is false.")]
        public T? Data { get; set; }

        /// <summary>List of field-level validation errors; null on success.</summary>
        [Description("List of field-level validation errors. Null on success; populated for 400 responses.")]
        public List<string>? Errors { get; set; }

        /// <summary>HTTP status code of the response.</summary>
        [Description("HTTP status code of the response (mirrors the response status line).")]
        public int StatusCode { get; set; } = 200;

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data, StatusCode = 200 };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = 400)
            => new() { Success = false, Message = message, Errors = errors, StatusCode = statusCode };
    }
}
