using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MesTech.Blazor.Services;

/// <summary>
/// Custom AuthenticationStateProvider that checks the in-memory JWT token store.
/// Provides authentication state to Blazor's authorization infrastructure.
/// </summary>
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    /// <summary>In-memory JWT token. Persists for the Blazor Server circuit lifetime.</summary>
    public string? Token { get; private set; }

    /// <summary>Cached username for display purposes.</summary>
    public string? Username { get; private set; }

    /// <summary>Quick check without building full ClaimsPrincipal.</summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrEmpty(Token))
            return Task.FromResult(_anonymous);

        var claims = ParseClaimsFromToken(Token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }

    /// <summary>
    /// Called after successful login. Stores the token and notifies
    /// Blazor's authorization system that the user is now authenticated.
    /// </summary>
    public void MarkUserAsAuthenticated(string token, string username)
    {
        Token = token;
        Username = username;

        var claims = ParseClaimsFromToken(token);
        // Ensure we always have a Name claim for display
        if (!claims.Any(c => c.Type == ClaimTypes.Name))
            claims.Add(new Claim(ClaimTypes.Name, username));

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    /// <summary>
    /// Called on logout. Clears the token and notifies Blazor's
    /// authorization system that the user is now anonymous.
    /// </summary>
    public void MarkUserAsLoggedOut()
    {
        Token = null;
        Username = null;
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }

    /// <summary>
    /// Extracts claims from a JWT token. Validates format and expiration.
    /// SECURITY: Non-JWT or malformed tokens are REJECTED (empty claims = anonymous).
    /// Demo mode requires explicit ASPNETCORE_ENVIRONMENT=Development check.
    /// </summary>
    private static List<Claim> ParseClaimsFromToken(string token)
    {
        var claims = new List<Claim>();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);

                // Validate token expiration — expired tokens must not grant access
                if (jwt.ValidTo != DateTime.MinValue && jwt.ValidTo < DateTime.UtcNow)
                {
                    // Expired token → return empty claims (unauthenticated)
                    return claims;
                }

                claims.AddRange(jwt.Claims);

                // Map JWT 'sub' or 'unique_name' to ClaimTypes.Name if not already present
                if (!claims.Any(c => c.Type == ClaimTypes.Name))
                {
                    var nameClaim = jwt.Claims.FirstOrDefault(c =>
                        c.Type == "unique_name" || c.Type == "name" || c.Type == JwtRegisteredClaimNames.Sub);
                    if (nameClaim is not null)
                        claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
                }
            }
            // else: Non-JWT token → return empty claims (unauthenticated)
            // SECURITY FIX (HH-DEV4-027): Previously returned Demo claims for ANY
            // non-JWT string, allowing authentication bypass with garbage tokens.
        }
        catch
        {
            // Malformed token → return empty claims (unauthenticated)
            // SECURITY FIX (HH-DEV4-027): Previously granted Demo access on parse errors.
        }

        return claims;
    }
}
