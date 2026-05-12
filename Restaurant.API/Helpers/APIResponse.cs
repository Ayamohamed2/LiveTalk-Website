using System.Net;

namespace Villa_API_Project.Models
{
    public class ApiResponse<T>
    {

        private static string GetMessageFromStatusCode(int statusCode)
        {
            return statusCode switch
            {
                200 => "Success",
                201 => "Created",
                204 => "No Content",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "Unknown Status Code"
            };
        }
        public int StatusCode { get; set; }
        public bool Success => StatusCode >= 200 && StatusCode < 300;
        public List<string>? Messages { get; set; }
        public T? Data { get; set; }

        public ApiResponse(int statusCode, string? message = null, T? data = default)
        {
            StatusCode = statusCode;
            Messages = message?.Split(',').ToList();
            Data = data;
        }
    }

}
