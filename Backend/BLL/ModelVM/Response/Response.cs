
namespace BLL.ModelVM.Response
{
    public record Response<T>(T result, string? errorMessage, bool IsHaveErrorOrNo);
}
