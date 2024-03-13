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

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, InvidiousAPIAccess invidiousAPIAccess, VideoDbContext videoDbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
        public async Task<IEnumerable<FoundChannel>> SearchChannels([FromQuery] string searchText)
        {
            if (searchText == null || searchText.Length < 2)
            {
                throw new ArgumentException("searchText must be at least length 2");
            }
            var invidiousResults = await _invidiousAPIAccess.Search(new Models.Invidious.SearchRequest
            {
                Q = searchText,
                Type = "channel",
            });
            var invidiousChannels = invidiousResults.OfType<Models.Invidious.SearchResponse_Channel>().Take(10).ToList();
            var uniqueIds = invidiousChannels.Select(z => z.AuthorId).ToList();
            if (!uniqueIds.Any())
            {
                return Enumerable.Empty<FoundChannel>();
            }
            var existingChannels = await _videoDbContext.ChannelVideoCounts.Where(z => uniqueIds.Contains(z.UniqueId)).ToListAsync();
            var response = invidiousChannels.Select(invidiousChannel =>
            {
                var existingChannel = existingChannels.FirstOrDefault(z => z.UniqueId == invidiousChannel.AuthorId);
                var thumbnail = invidiousChannel.AuthorThumbnails.Any(z => z.Height > 64)
                    ? invidiousChannel.AuthorThumbnails.OrderBy(z => z.Height).First().Url
                    : invidiousChannel.AuthorThumbnails.OrderByDescending(z => z.Height).FirstOrDefault()?.Url;
                if (thumbnail != null && thumbnail.StartsWith("//"))
                {
                    thumbnail = "https:" + thumbnail;
                }
                return new FoundChannel
                {
                    ChannelId = existingChannel?.ChannelId,
                    UniqueId = invidiousChannel.AuthorId,
                    Name = invidiousChannel.Author,
                    Handle = invidiousChannel.ChannelHandle,
                    Description = invidiousChannel.Description,
                    ThumbnailUrl = thumbnail,
                    VideoCount = existingChannel != null ? existingChannel.VideoCount : null //a tracked channel could have a null videoCount if we haven't scrapedToOldest
                };
            });
            return response;
        }

        [HttpGet("api/search-algorithms")]
        [Authorize]
        public async Task<IEnumerable<FoundAlgorithm>> SearchAlgorithms([FromQuery] string? username)
        {
            var algorithmsQuery = _videoDbContext.Algorithms.AsQueryable();
            if (!string.IsNullOrEmpty(username))
            {
                algorithmsQuery = algorithmsQuery.Where(z => z.Username == username);
            }
            var algorithms = await algorithmsQuery.ToListAsync();

            return algorithms.Select(z => new FoundAlgorithm
            {
                AlgorithmId = z.Id,
                AlgorithmName = z.Name,
                Description = z.Description,
                Username = z.Username
            });
        }

        [HttpGet("api/algorithm/{algorithmId}")]
        [Authorize]
        public async Task<LoadAlgorithmResult> GetAlgorithm([FromRoute] int algorithmId)
        {
            var algorithmEntity = _videoDbContext.Algorithms.First(z => z.Id == algorithmId);
            var itemInfos = await _videoDbContext.AlgorithmItemInfos.Where(z => z.AlgorithmId == algorithmId).ToListAsync();
            var result = new LoadAlgorithmResult
            {
                AlgorithmId = algorithmId,
                AlgorithmItems = itemInfos.Select(z => new LoadAlgorithmItem
                {
                    ChannelGroupId = z.ChannelGroupId,
                    ChannelId = z.ChannelId,
                    MaxChannelWeight = z.MaxChannelWeight,
                    WeightMultiplier = z.WeightMultiplier,
                    Name = z.Name,
                }),
                Description = algorithmEntity.Description,
                AlgorithmName = algorithmEntity.Name,
                Username = algorithmEntity.Username,
            };
            return result;
        }

        [HttpPut("api/algorithm")]
        [ProducesResponseType(typeof(int), 200)]
        [SwaggerResponse(400, "validation issues", typeof(string), "text/plain")]
        [Authorize]
        public async Task<IActionResult> UpdateAlgorithm([FromBody] UpdateAlgorithmRequest request)
        {
            var just1NonNull = request.AlgorithmItems.All(item => new object?[] { item.ChannelId, item.ChannelGroupId, item.NewChannel }.Count(p => p != null) == 1);
            if (!just1NonNull)
            {
                return BadRequest("each algorithmItem should have precisely 1 of the 3 nullable properties be non-null");
            }
            var channelsToAdd = request.AlgorithmItems.Where(z => !z.ChannelId.HasValue && !z.ChannelGroupId.HasValue).ToList();
            if (channelsToAdd.Any(z => z.NewChannel!.ChannelId.HasValue))
            {
                return BadRequest("NewChannel should be for new channels. The provided channel already has a channel Id");
            }
            if (channelsToAdd.Count() != channelsToAdd.Select(z => z.NewChannel!.UniqueId).Distinct().Count())
            {
                return BadRequest("NewChannels contains duplicate UniqueIds");
            }
            var username = User.FindFirst(ClaimTypes.Name).Value;
            var algorithm = request.AlgorithmId.HasValue
                ? _videoDbContext.Algorithms.Include(z => z.AlgorithmItems).First(z => z.Id == request.AlgorithmId)
                : new AlgorithmEntity()
                {
                    Name = request.Name,
                    Username = username,
                };
            if (!request.AlgorithmId.HasValue || algorithm.Name?.ToLower() != request.Name.ToLower())
            {
                //todo: scope it to user
                var nameConflicts = _videoDbContext.Algorithms.Where(z => z.Name == request.Name).Any();
                if (nameConflicts)
                {
                    return BadRequest("Algorithm Name already taken");
                }
            }

            //save all new channels to the database
            var newChannelEntities = channelsToAdd.Select(z => new ChannelEntity
            {
                Name = z.NewChannel!.Name,
                UniqueId = z.NewChannel!.UniqueId,
                Handle = z.NewChannel.Handle,
                DateLastScraped = null,
                ScrapedToOldest = false,
                ScrapeFailureCount = 0,
            }).ToList();
            _videoDbContext.Channels.AddRange(newChannelEntities);
            _videoDbContext.SaveChanges();//this should assign Ids to newChannelEntities


            if (!request.AlgorithmId.HasValue)
            {
                _videoDbContext.Algorithms.Add(algorithm);
            }
            algorithm.Name = request.Name;
            algorithm.Description = request.Description;

            //remove all algorithmItems not found among request.AlgorithmItems
            var includedChannelIds = request.AlgorithmItems.Where(z => z.ChannelId.HasValue).Select(z => z.ChannelId).ToList();
            var includedChannelGroupIds = request.AlgorithmItems.Where(z => z.ChannelGroupId.HasValue).Select(z => z.ChannelGroupId).ToList();
            if (algorithm.AlgorithmItems != null)
            {
                algorithm.AlgorithmItems = algorithm.AlgorithmItems
                    .Where(z => includedChannelGroupIds.Contains(z.ChannelGroupId) || includedChannelIds.Contains(z.ChannelId))
                    .ToList();
            } else
            {
                algorithm.AlgorithmItems = new List<AlgorithmItemEntity>();
            }



            //a malicious user could change the channel name or handle. Not sure if it's worth the performance cost to validate. If you have malicious users, you've got bigger problems
            var newChannelAlgorithmItems = channelsToAdd.Select(channelToAdd =>
            {
                var channelEntity = newChannelEntities.First(z => z.UniqueId == channelToAdd.NewChannel.UniqueId);
                return new AlgorithmItemEntity
                {
                    ChannelId = channelEntity.Id,//channelToAdd.NewChannel.Id is null, hence we use the channelEntity
                    MaxChannelWeight = channelToAdd.MaxChannelWeight,
                    WeightMultiplier = channelToAdd.WeightMultiplier,
                };
            }).ToList();
            newChannelAlgorithmItems.AddRange(request.AlgorithmItems
                .Where(z => z.NewChannel == null && !algorithm.AlgorithmItems.Any(zz => zz.ChannelId == z.ChannelId))
                .Select(z => new AlgorithmItemEntity
            {
                ChannelId = z.ChannelId,
                ChannelGroupId = z.ChannelGroupId,
                MaxChannelWeight = z.MaxChannelWeight,
                WeightMultiplier = z.WeightMultiplier,
            }));
            foreach(var newItem in newChannelAlgorithmItems)
            {
                algorithm.AlgorithmItems.Add(newItem);
            }
            _videoDbContext.SaveChanges();
            return Ok(algorithm.Id);
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
}