using System.Collections.Generic;

namespace Ecommerce.Core.Common
{
    public class PagedResult<T>
    {
        public IList<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages
        {
            get { return PageSize <= 0 ? 0 : (TotalCount + PageSize - 1) / PageSize; }
        }
        public PagedResult() { Items = new List<T>(); }
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public static OperationResult Ok(string message = null) { return new OperationResult { Success = true, Message = message }; }
        public static OperationResult Fail(string message) { return new OperationResult { Success = false, Message = message }; }
    }

    public class OperationResult<T> : OperationResult
    {
        public T Data { get; set; }
        public static OperationResult<T> Ok(T data, string message = null) { return new OperationResult<T> { Success = true, Data = data, Message = message }; }
        public new static OperationResult<T> Fail(string message) { return new OperationResult<T> { Success = false, Message = message }; }
    }
}
