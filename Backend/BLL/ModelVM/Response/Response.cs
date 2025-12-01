namespace BLL.ModelVM.Response
{
    public class Response<T>
    {
        public Response(T result, string? errorMessage, bool IsHaveErrorOrNo)
        {
            this.result = result;
            this.errorMessage = errorMessage;
            this.IsHaveErrorOrNo = IsHaveErrorOrNo;
            this.TotalCount = 0;
        }

        // Primary data (use lowercase names to match existing code and JSON naming)
        public T result { get; init; }
        public string? errorMessage { get; init; }
        // Existing code expects this exact name
        public bool IsHaveErrorOrNo { get; init; }

        // Pagination metadata
        public int TotalCount { get; set; }

        // Convenience property (not serialized specially)
        public bool Success => !IsHaveErrorOrNo;

        // Factory helpers
        public static Response<T> SuccessResponse(T value) => new Response<T>(value, null, false);
        public static Response<T> FailResponse(string error) => new Response<T>(default!, error, true);
    }
}
