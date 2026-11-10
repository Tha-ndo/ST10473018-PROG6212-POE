using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RaceDay.Api.Extensions;

public static class UserClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated user has no valid 'sub' claim.");
    }
}
