using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BloodDonationApp.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(string userId, string email, string role)
        {
            var secret = _configuration["Jwt:Secret"] ?? "DonoraBloodDonationAppSuperSecretJWTKey123!";
            var issuer = _configuration["Jwt:Issuer"] ?? "DonoraIssuer";
            var audience = _configuration["Jwt:Audience"] ?? "DonoraAudience";
            var expiryMin = double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMin),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var secret = _configuration["Jwt:Secret"] ?? "DonoraBloodDonationAppSuperSecretJWTKey123!";
            var issuer = _configuration["Jwt:Issuer"] ?? "DonoraIssuer";
            var audience = _configuration["Jwt:Audience"] ?? "DonoraAudience";

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidIssuer = issuer,
                ValidAudience = audience,
                ValidateLifetime = false // Here we explicitly say we don't care about token lifetime
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null)
            {
                Serilog.Log.Warning("Validated token is not a JwtSecurityToken. Type: {Type}", securityToken?.GetType().FullName);
            }
            else
            {
                Serilog.Log.Information("Validated token Alg: {Alg}, Expected Alg: {ExpectedAlg}", jwtSecurityToken.Header.Alg, SecurityAlgorithms.HmacSha256Signature);
            }

            if (securityToken is not JwtSecurityToken jwtToken || 
                (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase) &&
                 !jwtToken.Header.Alg.Equals("HS256", StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new SecurityTokenException($"Invalid token signature algorithm. Got: {jwtSecurityToken?.Header?.Alg}, Expected: {SecurityAlgorithms.HmacSha256Signature}");
            }

            return principal;
        }
    }
}
