using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace MyVidious.Controllers
{
    [Route("admin")]
    [ApiController]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Index()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
            return PhysicalFile(filePath, "text/html");
        }

        [HttpGet("api/user-info")]
        public async Task<UserInfo> GetUserInfo()
        {
            var userClaim = User.FindFirst(ClaimTypes.Name);
            IdentityUser? user = userClaim != null
                ? await _userManager.FindByNameAsync(userClaim.Value)
                : null;
            var anyUsers = user != null 
                ? true 
                : _userManager.Users.Any();
            var isAdmin = user != null
                ? (await _userManager.GetRolesAsync(user)).Any(z => z == "admin")
                : false;
            return new UserInfo
            {
                Username = user?.UserName,
                AnyUsers = anyUsers,
                IsAdmin = isAdmin
            };
        }

        [HttpPost("api/create-user")]
        [ProducesResponseType(typeof(UserInfo), 200)]
        [ProducesResponseType(typeof(string), 400, "text/plain")]
        public async Task<IActionResult> CreateUser([FromBody] CreateAccountRequest request)
        {
            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                return BadRequest("User already exists.");
            }
            var isAdmin = !_userManager.Users.Any();
            var user = new IdentityUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.Username,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var resultErrors = result.Errors.Select(e => e.Description);
                return BadRequest(string.Join("\n", resultErrors));
            }
            if (isAdmin)
            {
                var role = new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "admin",
                };
                await _roleManager.CreateAsync(role);
                await _userManager.AddToRoleAsync(user, "admin");
            }
            var userInfo = new UserInfo
            {
                Username = user.UserName,
                AnyUsers = true,
                IsAdmin = isAdmin
            };
            return Ok(userInfo);
        }

        [HttpPost("api/login")]
        [ProducesResponseType(typeof(UserInfo), 200)]
        [ProducesResponseType(typeof(string), 400, "text/plain")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));

                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Any(z => z == "admin");
                if (isAdmin)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                }

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                var userInfo = new UserInfo
                {
                    Username = user.UserName,
                    AnyUsers = true,
                    IsAdmin = isAdmin
                };
                return Ok(userInfo);
            }
            return BadRequest("Invalid Username or Password");
        }

        [HttpPost("api/logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok("Logout Successful");
        }

        [HttpPost("api/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = await _userManager.FindByNameAsync(User.FindFirst(ClaimTypes.Name)!.Value);
            var isValidPassword = await _userManager.CheckPasswordAsync(user!, request.OldPassword);
            if (!isValidPassword)
            {
                return BadRequest("Incorrect Password");
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (result.Succeeded)
            {
                return Ok("Password Changed");
            }
            return BadRequest(result.Errors);
        }


        [HttpGet("{filename}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetFile([FromRoute] string fileName)
        {
            // Ensure the requested file name is safe to use
            var safeFileName = Path.GetFileName(fileName);

            if (string.IsNullOrEmpty(safeFileName))
            {
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", safeFileName);

            if (System.IO.File.Exists(filePath))
            {
                string? contentType;
                var contentTypeProvider = new FileExtensionContentTypeProvider();
                if (!contentTypeProvider.TryGetContentType(filePath, out contentType))
                {
                    contentType = "application/octet-stream"; // Default MIME type if not found
                }

                return PhysicalFile(filePath, contentType);
            }
            else
            {
                return NotFound();
            }
        }
    }

    public class UserInfo
    {
        public string? Username { get; set; }
        public bool IsAdmin { get; set; }
        public bool AnyUsers { get; set; }
    }
}