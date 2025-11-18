using BLL.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BLL.Services.Impelementation
{
 public class TokenService : ITokenService
 {
 private readonly IConfiguration _config;
 public TokenService(IConfiguration config)
 {
 _config = config;
 }

 public string GenerateToken(Guid userId, string role, Guid? orderId = null, Guid? listingId = null)
 {
 var key = _config["Jwt:Key"];
 if (string.IsNullOrWhiteSpace(key))
 {
 throw new InvalidOperationException("Jwt:Key is not configured or is empty. Configure 'Jwt:Key' in appsettings or via environment/user secrets with a strong secret.");
 }
 var issuer = _config["Jwt:Issuer"];
 var audience = _config["Jwt:Audience"];
 var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

 var claims = new List<Claim>
 {
 new Claim("sub", userId.ToString()),
 new Claim(ClaimTypes.Role, role)
 };

 if (orderId.HasValue) claims.Add(new Claim("orderId", orderId.Value.ToString()));
 if (listingId.HasValue) claims.Add(new Claim("listingId", listingId.Value.ToString()));

 var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
 var cred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
 var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(expireMinutes), signingCredentials: cred);
 return new JwtSecurityTokenHandler().WriteToken(token);
 }
 }
}
