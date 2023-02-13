using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JWTSetUp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        UserManager<IdentityUser<int>> UserManager { get; set; }
        SignInManager<IdentityUser<int>> SignInManager { get; set; }
        IConfiguration Configuration { get; set; }

        public UserController(UserManager<IdentityUser<int>> userManager, SignInManager<IdentityUser<int>> signInManager, IConfiguration configuration)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            Configuration = configuration;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(APILoginRequest request)
        {
            var result = await LoginUser(request);          
            return Ok(new
            {
                result.Success,
                result.Message,
                result.Token
            });
            
        }

        private async Task<(bool Success,string Message, string Token)> LoginUser(APILoginRequest request)
        {
            try
            {
                var user = await UserManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return (false,"Invalid UserName Or Password!", "");
                }
                else
                {
                    
                    var result = await SignInManager.PasswordSignInAsync(user, request.Password, false, true);
                    if (result.Succeeded)
                    {
                        return (true, user.UserName, GenerateJWTToken(user));
                    }
                    else
                    {
                        if (result.IsLockedOut)
                        {
                            return (false, "User Is Locked Out!", "");
                        }
                        else
                        {
                            return (false, "Invalid UserName Or Password!", "");
                        }
                    }
                }
            }
            catch
            {
                return (false, "Exception Occured", "");
            }
        }

        private string GenerateJWTToken(IdentityUser<int> user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWTSecret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Configuration.GetValue<int>("JWTExpireDays"));

            var token = new JwtSecurityToken(
                Configuration["JWTIssuer"],
                Configuration["JWTIssuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
    public class APILoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
