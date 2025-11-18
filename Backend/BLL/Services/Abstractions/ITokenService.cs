namespace BLL.Services.Abstractions
{
 public interface ITokenService
 {
 string GenerateToken(Guid userId, string role, Guid? orderId = null, Guid? listingId = null);
 }
}
