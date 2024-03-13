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
using MyVidious.Models.Invidious;

namespace MyVidious.Controllers
{
    [ApiController]
    public class HomeController : Controller
    {
        private readonly VideoDbContext _videoDbContext;
        private readonly InvidiousAPIAccess _invidiousAPIAccess;

        public HomeController(InvidiousAPIAccess invidiousAPIAccess, VideoDbContext videoDbContext)
        {
            _videoDbContext = videoDbContext;
            _invidiousAPIAccess = invidiousAPIAccess;
        }
        [HttpGet("")]
        public async Task<IActionResult> GetRootPage()
        {
            var algorithms = await _videoDbContext.Algorithms.ToListAsync();
            var foundAlgorithms = algorithms.Select(z => new FoundAlgorithm
            {
                AlgorithmId = z.Id,
                AlgorithmName = z.Name.ToLower(),
                Description = z.Description,
                Username = z.Username.ToLower()
            }).ToList();
            return View("Root", foundAlgorithms);
        }
        [Route("{username}/{algorithmName}")]
        [HttpGet]
        public async Task<IActionResult> ViewAlgorithm([FromRoute] string username, [FromRoute] string algorithmName)
        {
            var algorithmEntity = _videoDbContext.Algorithms.FirstOrDefault(z => z.Username == username && z.Name == algorithmName);
            if (algorithmEntity == null)
            {
                return View("NotFound");
            }
            var itemInfos = await _videoDbContext.AlgorithmItemInfos.Where(z => z.AlgorithmId == algorithmEntity.Id).ToListAsync();
            var result = new LoadAlgorithmResult
            {
                AlgorithmId = algorithmEntity.Id,
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
            return View("Algorithm", result);
        }
    }
}