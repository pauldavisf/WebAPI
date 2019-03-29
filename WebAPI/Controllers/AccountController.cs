using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using System.Security.Claims;
using WebAPI.Models;
using WebAPI;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace TokenApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly UserContext _context;

        public AccountController(UserContext userContext)
        {
            _context = userContext;

            if (userContext.Users.Count() == 0)
            {
                var admin = new User
                {
                    Login = "admin",
                    Password = "@dmin",
                    Role = "admin"
                };

                _context.Users.Add(admin);
                _context.SaveChanges();
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register()
        {
            var username = Request.Form["username"];
            var password = Request.Form["password"];

            var userFromDb = _context.Users.FirstOrDefault(x => x.Login == username);

            if (userFromDb == null)
            {
                var user = new User
                {
                    Login = username,
                    Password = password,
                    Role = "user"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return user;
            }
            else
            {
                await Response.WriteAsync($"User with login {username} already exists in database");
                return null;
            }
        }

        [HttpPut("changePassword")]
        public async Task<IActionResult> ChangePassword()
        {
            var username = Request.Form["username"];
            var password = Request.Form["password"];
            var newPassword = Request.Form["newPassword"];

            var userFromDb = _context.Users.FirstOrDefault(x => x.Login == username && x.Password == password);

            if (userFromDb == null)
            {
                await Response.WriteAsync("Wrong username/password");
                return Unauthorized();
            }
            else
            {
                userFromDb.Password = newPassword;
                _context.Entry(userFromDb).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            if (User.Identity.Name != "admin")
            {
                await Response.WriteAsync("You must be admin");
                return Unauthorized();
            }

            return await _context.Users.ToListAsync();
        }

        [HttpDelete("{username}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string username)
        {
            if (User.Identity.Name != "admin")
            {
                await Response.WriteAsync("You must be admin");
                return Unauthorized();
            }

            var user = _context.Users.FirstOrDefault(x => x.Login == username);

            if (user == null)
            {
                await Response.WriteAsync($"User with login {username} doesn't exist in database");
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("token")]
        public async Task Token()
        {
            var username = Request.Form["username"];
            var password = Request.Form["password"];

            var identity = await GetIdentity(username, password);
            if (identity == null)
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Invalid username or password.");
                return;
            }

            var now = DateTime.UtcNow;

            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                username = identity.Name
            };

            Response.ContentType = "application/json";
            await Response.WriteAsync(JsonConvert.SerializeObject(response,
                new JsonSerializerSettings { Formatting = Formatting.Indented }));
        }

        private async Task<ClaimsIdentity> GetIdentity(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Login == username && x.Password == password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role)
                };
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
                return claimsIdentity;
            }

            // если пользователя не найдено
            return null;
        }
    }
}