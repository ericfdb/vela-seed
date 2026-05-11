using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace VelaSeed;

/// <summary>
/// Builds identity tokens for the VelaBridge Function App.
/// The Function App reads JWT claims to route requests but does NOT validate signatures.
/// </summary>
public static class Identity
{
    private const string Key = "placeholder-key-not-validated-by-function-app-00";
    private const string Issuer = "https://fdbvelaidentitynp.okta.com/oauth2/default";
    private const string Audience = "0oa4wnx0m8KDSy9Vs697";
    private const string OktaId = "00u1wscm37wnVQZ0y1d8";

    public static string BuildToken(string participantId, string customerType)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, OktaId),
                new Claim("CustomerParticipantId", participantId),
                new Claim("CustomerType", customerType),
                new Claim("groups", "Everyone"),
                new Claim("groups", "VelaBridgeUser"),
                new Claim("groups", "VelaBridgeAdmin"),
            ],
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return "Bearer " + new JwtSecurityTokenHandler().WriteToken(token);
    }
}
