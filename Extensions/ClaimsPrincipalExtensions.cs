using System.Security.Claims;

namespace ARTISTO.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(
        this ClaimsPrincipal principal,
        out int userId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var claimValue = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claimValue, out userId)
            && userId > 0;
    }
}
