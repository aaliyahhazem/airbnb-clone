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
 var expireMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m :1440; // default1 day
 var expiresAt = DateTime.UtcNow.AddMinutes(expireMinutes);

 var claims = new List<Claim>
 {
 new Claim("sub", userId.ToString()),
 // Include both standard role claim and 'role' so RoleClaimType mapping works regardless of configuration
 new Claim(ClaimTypes.Role, role),
 new Claim("role", role)
 };

 if (orderId.HasValue) claims.Add(new Claim("orderId", orderId.Value.ToString()));
 if (listingId.HasValue) claims.Add(new Claim("listingId", listingId.Value.ToString()));

 var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
 var cred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
 var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAt, signingCredentials: cred);
 return new JwtSecurityTokenHandler().WriteToken(token);
 }
 }
}
