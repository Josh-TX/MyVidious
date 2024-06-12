using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using MyVidious.Access;
using MyVidious.Data;
using MyVidious.Models.Admin;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Annotations;

namespace MyVidious.Controllers
{
    [Route("admin")]
    [ApiController]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly VideoDbContext _videoDbContext;
        private readonly InvidiousAPIAccess _invidiousAPIAccess;
        private readonly AdminAccess _adminAccess;

        public AdminController(
            UserManager<IdentityUser> userManager, 
            RoleManager<IdentityRole> roleManager, 
            AdminAccess adminAccess,
            InvidiousAPIAccess invidiousAPIAccess, 
            VideoDbContext videoDbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _adminAccess = adminAccess;
            _videoDbContext = videoDbContext;
            _invidiousAPIAccess = invidiousAPIAccess;
        }

        [HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Index()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
            return PhysicalFile(filePath, "text/html");
        }

        [HttpGet("api/search-channels")]
        [Authorize]
        public Task<IEnumerable<FoundChannel>> SearchChannels([FromQuery] string searchText)
        {
            return _adminAccess.SearchChannels(searchText);
        }

        [HttpGet("api/search-playlists")]
        [Authorize]
        public Task<IEnumerable<FoundPlaylist>> SearchPlaylists([FromQuery] string searchText)
        {
            return _adminAccess.SearchPlaylists(searchText);
        }

        [HttpGet("api/own-algorithms")]
        [Authorize]
        public Task<IEnumerable<FoundAlgorithm>> GetOwnAlgorithms()
        {
            var username = User.FindFirst(ClaimTypes.Name)!.Value;
            return _adminAccess.SearchAlgorithms(username, true);
        }

        [HttpGet("api/others-algorithms")]
        [Authorize]
        public Task<IEnumerable<FoundAlgorithm>> GetOthersAlgorithms()
        {
            var username = User.FindFirst(ClaimTypes.Name)!.Value;
            return _adminAccess.SearchAlgorithms(username, false);
        }

        [HttpGet("api/algorithm/{algorithmId}")]
        [Authorize]
        public Task<LoadAlgorithmResult> GetAlgorithm([FromRoute] int algorithmId)
        {
            return _adminAccess.GetAlgorithm(algorithmId);
        }
        [HttpDelete("api/algorithms/{algorithmId}")]
        [Authorize]
        public IActionResult DeleteAlgorithm([FromRoute] int algorithmId)
        {
            var username = User.FindFirst(ClaimTypes.Name)!.Value;
            _adminAccess.DeleteAlgorithm(algorithmId, username);
            return Ok();
        }

        [HttpPut("api/algorithm")]
        [ProducesResponseType(typeof(int), 200)]
        [SwaggerResponse(400, "validation issues", typeof(string), "text/plain")]
        [Authorize]
        public async Task<IActionResult> UpdateAlgorithm([FromBody] UpdateAlgorithmRequest request)
        {
            var username = User.FindFirst(ClaimTypes.Name)!.Value;
            var id = await _adminAccess.UpdateAlgorithm(request, username);
            return Ok(id);
        }


        [HttpGet("api/invite-codes")]
        [Authorize(Roles = "admin")]
        public async Task<IEnumerable<InviteCode>> GetInviteCodes()
        {
            var inviteCodes = _videoDbContext.InviteCodes.ToList();
            return inviteCodes.Select(z => new InviteCode
            {
                Code = z.Code,
                UsageCount = z.UsageCount,
                RemainingUses = z.RemainingUses
            });
        }

        [HttpPut("api/invite-codes")]
        [Authorize(Roles = "admin")]
        public IActionResult UpdateInviteCodes([FromBody] IEnumerable<InviteCode> inviteCodes)
        {
            //not the most effecient, but it doesn't need to be effecient
            var existingInviteCode = _videoDbContext.InviteCodes.ToList();
            _videoDbContext.InviteCodes.RemoveRange(existingInviteCode);
            _videoDbContext.InviteCodes.AddRange(inviteCodes.Select(z => new InviteCodeEntity
            {
                Code = z.Code,
                UsageCount = z.UsageCount,
                RemainingUses = z.RemainingUses
            }).ToList());
            _videoDbContext.SaveChanges();
            return Ok();
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
            var emptyInviteCode = _videoDbContext.InviteCodes.Any(z => z.Code == "");
            var anyInviteCode = emptyInviteCode ? true : _videoDbContext.InviteCodes.Any();
            return new UserInfo
            {
                Username = user?.UserName,
                AnyUsers = anyUsers,
                IsAdmin = isAdmin,
                OpenInvite = emptyInviteCode ? true : (anyInviteCode ? null : false)
            };
        }

        [HttpPost("api/create-user")]
        [ProducesResponseType(typeof(UserInfo), 200)]
        [SwaggerResponse(400, "validation issues", typeof(string), "text/plain")]
        public async Task<IActionResult> CreateUser([FromBody] CreateAccountRequest request)
        {
            if (request.Username.ToLower() == "admin")
            {
                return BadRequest("Username admin is not allowed");//since /admin is reserved for the admin interface
            }
            if (request.Username.Length < 3)
            {
                return BadRequest("Username must be at least 3 chars long");
            }
            var isAdmin = !_userManager.Users.Any();
            if (!isAdmin)
            { 
                //the following validations aren't need for the admin (first user)
                var existingUser = await _userManager.FindByNameAsync(request.Username);
                if (existingUser != null)
                {
                    return BadRequest("User already exists.");
                }
                request.InviteCode ??= "";
                var inviteCode = _videoDbContext.InviteCodes.FirstOrDefault(z => z.Code == request.InviteCode);
                if (inviteCode != null && inviteCode.RemainingUses > 0)
                {
                    inviteCode.RemainingUses--;
                    inviteCode.UsageCount++;
                }
                else
                {
                    return BadRequest("Invalid Invite Code");
                }
            }
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
            _videoDbContext.SaveChanges();//if the inviteCode remaining uses changed, this will save the change
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
            await SignInWithRoles(user, isAdmin);
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
        [SwaggerResponse(400, "validation issues", typeof(string), "text/plain")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Any(z => z == "admin");
                await SignInWithRoles(user, isAdmin);
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

        private async Task<bool> SignInWithRoles(IdentityUser user, bool isAdmin)
        {
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName!));
            if (isAdmin)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
            }
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return isAdmin;
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
            var result = await _userManager.ChangePasswordAsync(user!, request.OldPassword, request.NewPassword);
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
}