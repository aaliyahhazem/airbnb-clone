using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
 public abstract class BaseController : ControllerBase
 {
 // Helper to read user id from multiple possible claim names
 protected Guid? GetUserIdFromClaims()
 {
 var possible = new[]
 {
 ClaimTypes.NameIdentifier,
 "sub",
 JwtRegisteredClaimNames.Sub,
 "id",
 "uid"
 };

 foreach (var name in possible)
 {
 var claim = User.FindFirst(name)?.Value;
 if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var g))
 return g;
 }

 // sometimes NameIdentifier is numeric or string id used by Identity; try Name
 var nameClaim = User.Identity?.Name;
 if (!string.IsNullOrEmpty(nameClaim) && Guid.TryParse(nameClaim, out var byName))
 return byName;

 return null;
 }
 }
}
