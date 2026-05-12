using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEEFRA.Core.DTO.Service
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public string? ErrorType { get; set; }

        public static ServiceResult<T> Ok(T data, string message = "")
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message,
                ErrorType = null
            };
        }

        // ✅ Failure helper
        public static ServiceResult<T> Fail(string message, string errorType = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Data = default,
                Message = message,
                ErrorType = errorType
            };
        }
    }
}
