using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cw3.DTOs.Requests;
using Cw3.Models;
using Cw3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cw3.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private static readonly string ISSUER = "Gakko";
        private static readonly string AUDIENCE = "Students";


        public IConfiguration Configuration { get; set; }

        private readonly IDbService dbService;

        public AuthController(IConfiguration configuration, IDbService dbService)
        {
            Configuration = configuration;
            this.dbService = dbService;
        }

        [HttpPost("setpassword")]
        public IActionResult SetPassword(LoginRequest request)
        {   
            try
            {
                var oldHash = dbService.GetAuth(request.Login).Password;
                if (oldHash.Length != 0)
                {
                    throw new Exception("Password already set");
                }

                var salt = CreateSalt();
                var hash = CreateHash(request.Password, salt);

                dbService.SetPasswordHash(request.Login, hash, salt);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        private static string CreateHash(string password, string salt)
        {
            var valueBytes = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: Encoding.UTF8.GetBytes(salt),
                    prf: KeyDerivationPrf.HMACSHA512,
                    iterationCount: 20000,
                    numBytesRequested: 256 / 8
                );

            return Convert.ToBase64String(valueBytes);
        }

        private static string CreateSalt()
        {
            var randomBytes = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }

        private static bool ValidateHash(string password, string salt, string hash) => CreateHash(password, salt) == hash;

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var auth = dbService.GetAuth(request.Login);

            if (auth == null)
            {
                return BadRequest();
            }

            try {
                if (!ValidateHash(request.Password, auth.Salt, auth.Password))
                {
                    throw new Exception("Wrong password");
                }

                var student = dbService.GetStudent(request.Login);

                var roles = auth.Roles;
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, auth.IndexNumber),
                    new Claim(ClaimTypes.Name, $"{student.FirstName} {student.LastName}")
                };

                foreach (var role in roles)
                {
                    if (role.Length != 0)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                }

                var token = CreateToken(claims);
                var refreshToken = CreateRefreshToken();
                dbService.UpdateRefreshToken(student.IndexNumber, refreshToken);

                return Ok(new
                {
                    token,
                    refreshToken
                }); ;
            }
            catch (Exception e)
            {
                return Unauthorized(e.Message);
            }
        }

        private JwtSecurityToken CreateToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken
            (
                issuer: ISSUER,
                audience: AUDIENCE,
                claims: claims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: creds
            );
        }

        private static string CreateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        [HttpPost("refresh")]
        public IActionResult Refresh(RefreshTokenRequest request)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(request.Token);
                var username = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var savedRefreshToken = dbService.GetAuth(username).RefreshToken;
                if (savedRefreshToken != request.RefreshToken)
                    throw new SecurityTokenException("Invalid refresh token");

                var newJwtToken = CreateToken(principal.Claims);
                var newRefreshToken = CreateRefreshToken();
                dbService.UpdateRefreshToken(username, newRefreshToken);

                return Ok(new
                {
                    token = newJwtToken,
                    refreshToken = newRefreshToken
                });
            }
            catch (Exception e)
            {
                return Unauthorized(e.Message);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidAudience = AUDIENCE,
                ValidIssuer = ISSUER,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}
