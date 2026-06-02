namespace ContactSystem.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public int StatusCode { get; set; } = 200;

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data, StatusCode = 200 };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = 400)
            => new() { Success = false, Message = message, Errors = errors, StatusCode = statusCode };
    }
}
